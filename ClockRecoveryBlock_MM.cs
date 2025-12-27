using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace RX_SSDV
{
    public class ClockRecoveryBlock_MM
    {
        public PolyphaseFilterBank pfb;

        private float mu;
        private float muGain;
        private float omegaMid;
        private float omegaRelativeLimit;
        private float omegaLimit;
        private float omegaGain;

        private float p_2T;
        private float p_1T;
        private float p_0T;
        private float c_2T;
        private float c_1T;
        private float c_0T;

        public ClockRecoveryBlock_MM(float mu, float muGain, float omega, float omegaGain, float omegaLimit, int nFilter, int nTaps)
        {
            this.mu = mu;
            this.muGain = muGain;
            omegaRelativeLimit = omegaLimit;
            omegaMid = omega;
            omegaLimit = omegaRelativeLimit * omega;
            this.omegaGain = omegaGain;

            pfb = new PolyphaseFilterBank(RootRaisedCosine(0.001, 48000 / 5.0, 2, 0.35, nTaps), nTaps);
        }

        /// <summary>
        /// Process the clock recover.
        /// </summary>
        /// <param name="inputSamplesI">I channel of input samples(Real part)</param>
        /// <param name="inputSamplesQ">Q channel of input samples(Imag part)</param>
        /// <param name="outputSamplesI">I channel of output samples(Real part)</param>
        /// <param name="outputSamplesQ">Q channel of output samples(Imag part)</param>
        public void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {

        }

        public static double[] RootRaisedCosine(double gain, double sampling_freq, double symbol_rate, double alpha, int ntaps)
        {
            ntaps |= 1; // ensure that ntaps is odd

            double spb = sampling_freq / symbol_rate; // samples per bit/symbol
            double[] taps = new double[ntaps];
            double scale = 0;
            for (int i = 0; i < ntaps; i++)
            {
                double x1, x2, x3, num, den;
                double xindx = i - ntaps / 2;
                x1 = PI * xindx / spb;
                x2 = 4 * alpha * xindx / spb;
                x3 = x2 * x2 - 1;

                if (Abs(x3) >= 0.000001)
                { // Avoid Rounding errors...
                    if (i != ntaps / 2)
                        num = Cos((1 + alpha) * x1) +
                              Sin((1 - alpha) * x1) / (4 * alpha * xindx / spb);
                    else
                        num = Cos((1 + alpha) * x1) + (1 - alpha) * PI / (4 * alpha);
                    den = x3 * PI;
                }
                else
                {
                    if (alpha == 1)
                    {
                        taps[i] = -1;
                        continue;
                    }
                    x3 = (1 - alpha) * x1;
                    x2 = (1 + alpha) * x1;
                    num = (Sin(x2) * (1 + alpha) * PI -
                        Cos(x3) * ((1 - alpha) * PI * spb) / (4 * alpha * xindx) +
                        Sin(x3) * spb * spb / (4 * alpha * xindx * xindx));
                    den = -32 * PI * alpha * alpha * xindx / spb;
                }
                taps[i] = 4 * alpha * num / den;
                scale += taps[i];
            }

            for (int i = 0; i < ntaps; i++)
                taps[i] = taps[i] * gain / scale;

            return taps;
        }
    }
}
