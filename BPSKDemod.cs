using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using NWaves.Utils;
using RX_SSDV.Utils;
using NWaves.Filters.Base;

namespace RX_SSDV
{
    public class BPSKDemod
    {
        public CostasLoop costasLoop;
        public LMS_DD_Equalizer equalizer;
        public ClockRecoveryBlock_MM clockRecovery;
        public FeedforwardAGC agc;
        public ComplexFirFilter rrcFilter;

        private float[] outBufferI_1;
        private float[] outBufferQ_1;
        private float[] outBufferI_2;
        private float[] outBufferQ_2;
        //private int bufferId = 1;

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
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit,
            float agcGain, float agcRef)
        {
            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit);
            InitAGC(agcGain, agcRef);
            //InitRrcFilter();
        }

        public BPSKDemod(float costasBw, float costasFreqLimit,
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit,
            float agcGain, float agcRef,
            int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit);
            InitAGC(agcGain, agcRef);
            //InitRrcFilter();
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

        public void InitClockSync(float mu, float muGain, float omega, float omegaGain, float omegaLimit)
        {
            clockRecovery = new ClockRecoveryBlock_MM(mu, muGain, omega, omegaGain, omegaLimit);
        }

        public void InitAGC(float gain, float reference)
        {
            agc = new FeedforwardAGC(gain, reference);
        }

        public void InitRrcFilter(int sampleRate, int symbolRate, int sps)
        {
            float[] rrcTaps = FilterUtils.RootRaisedCosine(16, 1, 1 / sps, 0.35f, 11 * MainDSP.SamplePerSymbol);
            rrcFilter = new ComplexFirFilter(rrcTaps, new float[rrcTaps.Length]);
        }
        
        public void InitModulesDefault()
        {
            InitCostas(0.05f, 10);
            InitEqualizer(0.05f, 2, 2);
            InitClockSync(0.5f, 0.175f, MainDSP.SamplePerSymbol, 0.75f * 0.75f, 0.05f);
            InitAGC(1, 1);
            //InitRrcFilter();
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
        public void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount)
        {
            CheckProcessOutputArr(realSignal.Length);
            CheckBlocks();

            //rrcFilter.ProcessOnline(realSignal, imagSignal, outBufferI_1, outBufferQ_1);

            int costasOutputSize = costasLoop.Process(realSignal.Length, realSignal, imagSignal, outBufferI_1, outBufferQ_1);

            int clockOutputSize = clockRecovery.Process(costasOutputSize, outBufferI_1, outBufferQ_1, outBufferI_2, outBufferQ_2);

            //int agcOutputSize = agc.Process(clockOutputSize, outBufferI_2, outBufferQ_2, outBufferI_1, outBufferQ_1);

            //int equalizerOutputSize = equalizer.Process(clockOutputSize, outBufferI_2, outBufferQ_2, outBufferI_1, outBufferQ_1);

            outputCount = clockOutputSize;
            //outputCount = realSignal.Length;
            
            outBufferI_2.FastCopyTo(outReal, outputCount);
            outBufferQ_2.FastCopyTo(outImag, outputCount);
            //outBufferI_2.FastCopyTo(outReal, clockOutputSize);
            //outBufferQ_2.FastCopyTo(outImag, clockOutputSize);
        }

        public void CheckBlocks()
        {
            if (costasLoop == null)
                throw new NullReferenceException("Costas loop not initialized");
            if (clockRecovery == null)
                throw new NullReferenceException("Clock recovery block not initialized");
            if (equalizer == null)
                throw new NullReferenceException("LMS equalizer not initialized");
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
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outBufferI_1, arrSize) || ArrayUtil.CheckNeedUpdate(outBufferQ_1, arrSize))
            {
                outBufferI_1 = new float[arrSize];
                outBufferQ_1 = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(outBufferI_2, arrSize) || ArrayUtil.CheckNeedUpdate(outBufferQ_2, arrSize))
            {
                outBufferI_2 = new float[arrSize];
                outBufferQ_2 = new float[arrSize];
            }
        }

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
