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

        private float[] outCostasI;
        private float[] outCostasQ;
        private float[] outEqualizerI;
        private float[] outEqualizerQ;
        private float[] cuttedEqualizerI;
        private float[] cuttedEqualizerQ;

        public BPSKDemod()
        {
            costasLoop = new CostasLoop(0.005f, 10);
            equalizer = LMS_DD_Equalizer.BuildEqualizer(0.05f, 2, 2);
        }

        public BPSKDemod(float costasBw, float costasFreqLimit, float equalizerGain, int equalizerKernelSize, int equalizerSPS)
        {
            costasLoop = new CostasLoop(costasBw, costasFreqLimit);
            equalizer = LMS_DD_Equalizer.BuildEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
        }

        public void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag)
        {
            CheckProcessOutputArr(realSignal.Length);
            //float[] outCostasI = new float[realSignal.Length];
            //float[] outCostasQ = new float[imagSignal.Length];
            ProcessCostas(realSignal, imagSignal, outCostasI, outCostasQ);

            //outReal = outCostasI;
            //outImag = outCostasQ;

            //float[] outEqualizerI = new float[outCostasI.Length];
            //float[] outEqualizerQ = new float[outCostasQ.Length];
            int equalizerOutputLength = ProcessEqualizer(outCostasI, outCostasQ, outEqualizerI, outEqualizerQ);

            outEqualizerI.FastCopyTo(outReal, equalizerOutputLength);
            outEqualizerQ.FastCopyTo(outImag, equalizerOutputLength);

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

        public void ProcessCostas(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            costasLoop.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        public int ProcessEqualizer(float[] realSignal, float[] imagSignal, float[] outRealSignal, float[] outImagSignal)
        {
            return equalizer.Process(realSignal, imagSignal, outRealSignal, outImagSignal);
        }

        public int CalcOutputSize(int inputSize)
        {
            return equalizer.CalcOutputSize(inputSize);
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
