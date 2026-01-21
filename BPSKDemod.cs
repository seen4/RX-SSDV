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
        public FeedforwardAGC agc;

        private float[] outCostasI;
        private float[] outCostasQ;
        private float[] outEqualizerI;
        private float[] outEqualizerQ;
        private float[] outClockSyncI;
        private float[] outClockSyncQ;
        private float[] outAgcI;
        private float[] outAgcQ;

        private static float imaginaryPoint = 1;

        public BPSKDemod()
        {
            InitModulesDefault();
        }

        public BPSKDemod(int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitModulesDefault();
        }

        public BPSKDemod(float costasBw, float costasFreqLimit, 
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit, int clockNFilt, int clockNTaps,
            float agcGain, float agcRef)
        {
            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit, clockNFilt, clockNTaps);
            InitAGC(agcGain, agcRef);
        }

        public BPSKDemod(float costasBw, float costasFreqLimit,
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit, int clockNFilt, int clockNTaps,
            float agcGain, float agcRef,
            int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit, clockNFilt, clockNTaps);
            InitAGC(agcGain, agcRef);
        }


        #region Module Define
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

        public void InitAGC(float gain, float reference)
        {
            agc = new FeedforwardAGC(gain, reference);
        }

        public void InitModulesDefault()
        {
            InitCostas(0.05f, 10);
            InitEqualizer(0.05f, 2, 2);
            InitClockSync(5, 0.75f, MainDSP.SamplePerSymbol, 0.75f * 0.75f, 0.05f, 128, 128 * 128);
            InitAGC(1, 1);
        }
        #endregion

        /// <summary>
        /// Process BPSK demodulation.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outReal">Output signal I(Real)</param>
        /// <param name="outImag">Output signal Q(Imag)</param>
        /// <param name="outputCount">Output array count</param>
        /// <param name="cutArray">Cut output array</param>
        public void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount, bool cutArray = false)
        {
            CheckProcessOutputArr(realSignal.Length);

            ProcessCostas(realSignal, imagSignal, outCostasI, outCostasQ);

            //outCostasI.FastCopyTo(outReal, outCostasI.Length);
            //outCostasQ.FastCopyTo(outImag, outCostasQ.Length);

            outputCount = ProcessClockSync(outCostasI, outCostasQ, outClockSyncI, outClockSyncQ);

            ProcessAGC(outClockSyncI, outClockSyncQ, outAgcI, outAgcQ);
            //outAgcI.FastCopyTo(outReal, outAgcI.Length);
            //outAgcQ.FastCopyTo(outImag, outAgcQ.Length);

            ProcessEqualizer(outAgcI, outAgcQ, outEqualizerI, outEqualizerQ);

            if (cutArray)
            {
                outEqualizerI.FastCopyTo(outReal, outputCount);
                outEqualizerQ.FastCopyTo(outImag, outputCount);
            }
            else
            {
                outEqualizerI.FastCopyTo(outReal, outEqualizerI.Length);
                outEqualizerQ.FastCopyTo(outImag, outEqualizerQ.Length);
            }
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
        /// <returns>Output samples array count</returns>
        public int ProcessClockSync(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if (clockRecovery == null)
                throw new NullReferenceException("Clock recovery block not initialized");
            return clockRecovery.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        /// <summary>
        /// Process equalizer.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outRealSignal">Output signal I(Real)</param>
        /// <param name="outImagSignal">Output signal Q(Imag)</param>
        /// <returns>Equalizer output array size</returns>
        public void ProcessEqualizer(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if (equalizer == null)
                throw new NullReferenceException("LMS equalizer not initialized");
            equalizer.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        /// <summary>
        /// Process AGC.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outRealSignal">Output signal I(Real)</param>
        /// <param name="outImagSignal">Output signal Q(Imag)</param>
        public void ProcessAGC(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            if(agc == null)
                throw new NullReferenceException("Feedforward AGC not initialized");
            agc.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        public float CalcAvgMagnitude(float[] samplesI, float[] samplesQ)
        {
            float positiveAvg = 0;
            float negativeAvg = 0;
            for (int i = 0; i < samplesI.Length; i++)
            {
                float sampleI = samplesI[i];
                float sampleQ = samplesQ[i];
                if (sampleI >= 0)
                {
                    positiveAvg += sampleI;
                }
                else
                {
                    negativeAvg += sampleQ;
                }
            }

            positiveAvg /= samplesI.Length;
            negativeAvg /= samplesQ.Length;

            return (positiveAvg - negativeAvg) / 2;
        }

        public void NormalizeSignal(float[] channelI, float[] channelQ)
        {
            if (channelI.Length != channelQ.Length)
                return;

            for(int i = 0; i < channelI.Length; i++)
            {
                float sampleI = channelI[i];
                float sampleQ = channelQ[i];
                float magnitude = MathF.Sqrt(sampleI * sampleI + sampleQ * sampleQ);
                if (magnitude == 0)
                    continue;
                channelI[i] = sampleI / magnitude;
                channelQ[i] = sampleQ / magnitude;
            }
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

            if (ArrayUtil.CheckNeedUpdate(outAgcI, arrSize) || ArrayUtil.CheckNeedUpdate(outAgcQ, arrSize))
            {
                outAgcI = new float[arrSize];
                outAgcQ = new float[arrSize];
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
        public static float BpskDecisionMaker(Complex sample)
        {
            //return sample.Real > 0 ? 1: 0;
            return sample.Real > 0 ? 1 * imaginaryPoint : -1 * imaginaryPoint;
        }

        /// <summary>
        /// Decision maker of BPSK modulation.
        /// </summary>
        /// <param name="sample">The real part of complex sample, or I channel of the signal.</param>
        /// <returns>Decision</returns>
        public static float BpskDecisionMaker(float sample)
        {
            //return sample > 0 ? 1 : 0;
            return sample > 0 ? 1 * imaginaryPoint : -1 * imaginaryPoint;
        }
    }
}
