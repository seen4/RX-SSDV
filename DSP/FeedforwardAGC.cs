using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public class FeedforwardAGC : DspBlock
    {
        float gain;
        float dReference;
        int processCount = 16;

        public FeedforwardAGC(float gain, float reference) : base()
        {
            this.gain = gain;
            dReference = reference;
        }

        public FeedforwardAGC(float gain, float reference, int processCount)
        {
            this.gain = gain;
            dReference = reference;
            this.processCount = processCount;
            SetHistory(processCount * 2);
        }

        /// <summary>
        /// Calcucate envelope
        /// </summary>
        /// <param name="x">Input sample</param>
        /// <returns>Envelope</returns>
        public static float Envelope(Complex x)
        {
            float rAbs = (float)Math.Abs(x.Real);
            float iAbs = (float)Math.Abs(x.Imaginary);

            if (rAbs > iAbs)
                return rAbs + 0.4f * iAbs;
            else
                return iAbs + 0.4f * rAbs;
        }

        /// <summary>
        /// Calcucate envelope
        /// </summary>
        /// <param name="real">Real part input sample</param>
        /// <param name="imag">Imag part input sample</param>
        /// <returns>Envelope</returns>
        //public static float Envelope(float real, float imag)
        //{
        //    //Approximate sqrt(x^2 + y^2)
        //    float rAbs = MathF.Abs(real);
        //    float iAbs = MathF.Abs(imag);

        //    if (rAbs > iAbs)
        //        return rAbs + 0.4f * iAbs;
        //    else
        //        return iAbs + 0.4f * rAbs;
        //}

        /// <summary>
        /// Process signal.
        /// </summary>
        /// <param name="inputSamplesI">Input samples(Real part)</param>
        /// <param name="inputSamplesQ">Input samples(Imag part)</param>
        /// <param name="outputSamplesI">Output samples(Real part)</param>
        /// <param name="outputSamplesQ">Output samples(Imag part)</param>
        public override int Process(int inputSize, float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {
            base.Process(inputSamplesI, inputSamplesQ, outputSamplesI, outputSamplesQ, inputSize);
            int outputCount = inputSize;

            for(int i = 0; i < inputSize; i++)
            {
                float maxEnv = 1e-4f;

                // Check whether the remaining sample count is enough.
                if(i + processCount > historyBuffer.Length) // i + (processCount - 1) > historyBuffer.Length - 1
                {
                    outputCount = i + 1;
                    break;
                }

                // Calc max envelpoe
                for (int j = 0; j < processCount; j++)
                {
                    Complex sample = historyBuffer[i + j];
                    maxEnv = MathF.Max(maxEnv, Envelope(sample));
                }

                // Calc gain & apply to output
                gain = dReference / maxEnv;
                outputSamplesI[i] = gain * inputSamplesI[i];
                outputSamplesQ[i] = gain * inputSamplesQ[i];
            }

            CompleteProcess(outputCount);
            return outputCount;
        }
    }
}
