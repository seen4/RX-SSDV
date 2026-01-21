using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV
{
    public class FeedforwardAGC : IDspBlock
    {
        float gain;
        float dReference;
        int processCount = 16;

        public FeedforwardAGC(float gain, float reference)
        {
            this.gain = gain;
            dReference = reference;
        }

        public FeedforwardAGC(float gain, float reference, int processCount)
        {
            this.gain = gain;
            dReference = reference;
            this.processCount = processCount;
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
        public static float Envelope(float real, float imag)
        {
            //Approximate sqrt(x^2 + y^2)
            float rAbs = MathF.Abs(real);
            float iAbs = MathF.Abs(imag);

            if (rAbs > iAbs)
                return rAbs + 0.4f * iAbs;
            else
                return iAbs + 0.4f * rAbs;
        }

        /// <summary>
        /// Process signal.
        /// </summary>
        /// <param name="inputSamplesI">Input samples(Real part)</param>
        /// <param name="inputSamplesQ">Input samples(Imag part)</param>
        /// <param name="outputSamplesI">Output samples(Real part)</param>
        /// <param name="outputSamplesQ">Output samples(Imag part)</param>
        public void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {
            for(int i = 0; i < inputSamplesI.Length; i++)
            {
                float maxEnv = 1e-4f;
                for (int j = 0; j < processCount; j++)
                {
                    if (i + j > inputSamplesI.Length - 1)
                        break;
                    
                    maxEnv = MathF.Max(maxEnv, Envelope(inputSamplesI[i + j], inputSamplesQ[i + j]));
                }
                gain = dReference / maxEnv;
                outputSamplesI[i] = gain * inputSamplesI[i];
                outputSamplesQ[i] = gain * inputSamplesQ[i];
            }
        }
    }
}
