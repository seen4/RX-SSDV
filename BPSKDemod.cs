using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using NWaves.Utils;
using RX_SSDV.Utils;

namespace RX_SSDV
{
    public class BPSKDemod
    {
        public CostasLoop costasLoop;
        public LMS_DD_Equalizer equalizer;
        public ClockRecoveryBlock_MM clockRecovery;

        private float[] outCostasI;
        private float[] outCostasQ;
        private float[] outEqualizerI;
        private float[] outEqualizerQ;
        private float[] outClockSyncI;
        private float[] outClockSyncQ;
        //private float[] cuttedEqualizerI;
        //private float[] cuttedEqualizerQ;

        public BPSKDemod()
        {
            InitCostas(0.05f, 10);
            InitEqualizer(0.05f, 2, 2);
            InitClockSync(5, 0.1621256f, MainDSP.SamplePerSymbol, 0.0072956f, 0.05f, 8, 128 * 8);
        }

        public BPSKDemod(int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitCostas(0.05f, 10);
            InitEqualizer(0.05f, 2, 2);
            InitClockSync(5, 0.1621256f, MainDSP.SamplePerSymbol, 0.0072956f, 0.05f, 8, 128 * 8);
        }

        public BPSKDemod(float costasBw, float costasFreqLimit, 
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit, int clockNFilt, int clockNTaps)
        {
            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit, clockNFilt, clockNTaps);
        }

        public BPSKDemod(float costasBw, float costasFreqLimit,
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit, int clockNFilt, int clockNTaps,
            int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit, clockNFilt, clockNTaps);
        }

        public void InitCostas(float bw, float freqLimit)
        {
            costasLoop = new CostasLoop(bw, freqLimit);
        }

        public void InitEqualizer(float gain, int kernelSize, int sps)
        {
            equalizer = LMS_DD_Equalizer.BuildEqualizer(gain, kernelSize, sps);
        }

        public void InitClockSync(float mu, float muGain, float omega, float omegaGain, float omegaLimit, int nFilt, int nTaps)
        {
            clockRecovery = new ClockRecoveryBlock_MM(mu, muGain, omega, omegaGain, omegaLimit, nFilt, nTaps);
        }

        /// <summary>
        /// Process BPSK demodulation.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outReal">Output signal I(Real)</param>
        /// <param name="outImag">Output signal Q(Imag)</param>
        public void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag)
        {
            CheckProcessOutputArr(realSignal.Length);

            ProcessCostas(realSignal, imagSignal, outCostasI, outCostasQ);

            ProcessClockSync(outCostasI, outCostasQ, outClockSyncI, outClockSyncQ);

            //outClockSyncI.FastCopyTo(outReal, outClockSyncI.Length);
            //outClockSyncQ.FastCopyTo(outImag, outClockSyncQ.Length);

            ProcessEqualizer(outClockSyncI, outClockSyncQ, outEqualizerI, outEqualizerQ);

            outEqualizerI.FastCopyTo(outReal, outEqualizerI.Length);
            outEqualizerQ.FastCopyTo(outImag, outEqualizerQ.Length);

            //if (useEqualizerCut)
            //{
            //    //float[] cuttedEqualizerI = new float[equalizerOutputLength];
            //    //float[] cuttedEqualizerQ = new float[equalizerOutputLength];

            //    CheckEqualizerCutArr(equalizerOutputLength);

            //    outEqualizerI.FastCopyTo(cuttedEqualizerI, equalizerOutputLength);
            //    outEqualizerQ.FastCopyTo(cuttedEqualizerQ, equalizerOutputLength);

            //    outReal = cuttedEqualizerI;
            //    outImag = cuttedEqualizerQ;

            //    return;
            //}

            //outReal = outEqualizerI;
            //outImag = outEqualizerQ;
        }

        /// <summary>
        /// Process costas loop.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outRealSignal">Output signal I(Real)</param>
        /// <param name="outImagSignal">Output signal Q(Imag)</param>
        public void ProcessCostas(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if (costasLoop == null)
                throw new NullReferenceException("Costas loop not initialized");
            costasLoop.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        /// <summary>
        /// Process clock recovery block.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outRealSignal">Output signal I(Real)</param>
        /// <param name="outImagSignal">Output signal Q(Imag)</param>
        public void ProcessClockSync(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if (clockRecovery == null)
                throw new NullReferenceException("Clock recovery block not initialized");
            clockRecovery.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        /// <summary>
        /// Process equalizer.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outRealSignal">Output signal I(Real)</param>
        /// <param name="outImagSignal">Output signal Q(Imag)</param>
        /// <returns>Equalizer output array size</returns>
        public int ProcessEqualizer(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if (equalizer == null)
                throw new NullReferenceException("LMS equalizer not initialized");
            return equalizer.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        /// <summary>
        /// Calcucate equalizer output array size.
        /// </summary>
        /// <param name="inputSize">Input array size</param>
        /// <returns>Output array size</returns>
        public int CalcOutputSize(int inputSize)
        {
            return clockRecovery.CalcOutputSize(inputSize);
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outCostasI, arrSize) || ArrayUtil.CheckNeedUpdate(outCostasQ, arrSize))
            {
                outCostasI = new float[arrSize];
                outCostasQ = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(outClockSyncI, arrSize) || ArrayUtil.CheckNeedUpdate(outClockSyncQ, arrSize))
            {
                outClockSyncI = new float[arrSize];
                outClockSyncQ = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(outEqualizerI, arrSize) || ArrayUtil.CheckNeedUpdate(outEqualizerQ, arrSize))
            {
                outEqualizerI = new float[arrSize];
                outEqualizerQ = new float[arrSize];
            }
        }

        //public void CheckEqualizerCutArr(int arrSize)
        //{
        //    if (cuttedEqualizerI == null || cuttedEqualizerQ == null || cuttedEqualizerI.Length != arrSize || cuttedEqualizerQ.Length != arrSize)
        //    {
        //        cuttedEqualizerI = new float[arrSize];
        //        cuttedEqualizerQ = new float[arrSize];
        //    }
        //}

        /// <summary>
        /// Decision maker of BPSK modulation.
        /// </summary>
        /// <param name="sample">The complex sample.</param>
        /// <returns>Decision</returns>
        public static int BpskDecisionMaker(Complex sample)
        {
            return sample.Real > 0 ? 1 : 0;
        }

        /// <summary>
        /// Decision maker of BPSK modulation.
        /// </summary>
        /// <param name="sample">The real part of complex sample, or I channel of the signal.</param>
        /// <returns>Decision</returns>
        public static int BpskDecisionMaker(float sample)
        {
            return sample > 0 ? 1 : 0;
        }
    }
}
