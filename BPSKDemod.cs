using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using NWaves.Utils;

namespace RX_SSDV
{
    public class BPSKDemod
    {
        public CostasLoop costasLoop;
        public LMS_DD_Equalizer equalizer;

        public BPSKDemod()
        {
            costasLoop = new CostasLoop(0.005f, 10);
            equalizer = LMS_DD_Equalizer.BuildEqualizer(0.05f, 25, 2);
        }

        public BPSKDemod(float costasBw, float costasFreqLimit, float equalizerGain, int equalizerKernelSize, int equalizerSPS)
        {
            costasLoop = new CostasLoop(costasBw, costasFreqLimit);
            equalizer = LMS_DD_Equalizer.BuildEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
        }

        public void Process(float[] realSignal, float[] imagSignal, out float[] outReal, out float[] outImag, bool useEqualizerCut = false)
        {
            float[] outCostasI = new float[realSignal.Length];
            float[] outCostasQ = new float[imagSignal.Length];
            ProcessCostas(realSignal, imagSignal, outCostasI, outCostasQ);

            //outReal = outCostasI;
            //outImag = outCostasQ;

            float[] outEqualizerI = new float[outCostasI.Length];
            float[] outEqualizerQ = new float[outCostasQ.Length];
            int equalizerOutputLength = ProcessEqualizer(outCostasI, outCostasQ, outEqualizerI, outEqualizerQ);

            if (useEqualizerCut)
            {
                float[] cuttedEqualizerI = new float[equalizerOutputLength];
                float[] cuttedEqualizerQ = new float[equalizerOutputLength];

                outEqualizerI.FastCopyTo(cuttedEqualizerI, equalizerOutputLength);
                outEqualizerQ.FastCopyTo(cuttedEqualizerQ, equalizerOutputLength);

                outReal = cuttedEqualizerI;
                outImag = cuttedEqualizerQ;

                return;
            }

            outReal = outEqualizerI;
            outImag = outEqualizerQ;
        }

        public void ProcessCostas(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            costasLoop.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        public int ProcessEqualizer(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            return equalizer.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

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
