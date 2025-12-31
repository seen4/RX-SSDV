using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using static RX_SSDV.CostasLoop;

namespace RX_SSDV
{
    public class ClockRecoveryBlock_MM
    {
        public PolyphaseFilterBank pfb;

        private int inc, ouc;
        private float phaseError;

        private float mu;
        private float muGain;
        private float omega;
        private float omegaMid;
        private float omegaRelativeLimit;
        private float omegaLimit;
        private float omegaGain;

        private Complex p_2T;
        private Complex p_1T;
        private Complex p_0T;
        private Complex c_2T;
        private Complex c_1T;
        private Complex c_0T;

        public ClockRecoveryBlock_MM(float mu, float muGain, float omega, float omegaGain, float omegaLimit, int nFilter, int nTaps)
        {
            this.mu = mu;
            this.muGain = muGain;
            this.omega = omega;
            this.omegaGain = omegaGain;
            omegaRelativeLimit = omegaLimit;

            omegaMid = omega;
            omegaLimit = omegaRelativeLimit * omega;

            pfb = new PolyphaseFilterBank(RootRaisedCosine(0.001, 48000 / 5.0, 2, 0.35, nTaps), nTaps);
        }

        public void UpdatePFB(int sampleRate, int nTaps)
        {
            pfb = new PolyphaseFilterBank(RootRaisedCosine(0.001, sampleRate / 5.0, 2, 0.35, nTaps), nTaps);
        }

        /// <summary>
        /// Process one sample.
        /// </summary>
        /// <param name="inputSamplesI">I channel of input samples(Real part)</param>
        /// <param name="inputSamplesQ">Q channel of input samples(Imag part)</param>
        /// <param name="outputSamplesI">I channel of output samples(Real part)</param>
        /// <param name="outputSamplesQ">Q channel of output samples(Imag part)</param>
        public void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {
            if (inputSamplesI.Length != inputSamplesQ.Length)
                throw new ArgumentException("inputSamplesI.Length mush equals inputSamplesQ.Length");

            // See Satdump:clock_recovery_mm.cpp

            for (int i = 0; i < inputSamplesI.Length && inc < inputSamplesI.Length; i++)
            {
                // Propagate delay
                p_2T = p_1T;
                p_1T = p_0T;
                c_2T = c_1T;
                c_1T = c_0T;

                //ouc = 0;

                // Compute output
                int imu = (int)MathF.Round(mu * pfb.FilterCount);
                if (imu < 0) // If we're out of bounds, clamp
                    imu = 0;
                if (imu >= pfb.FilterCount)
                    imu = pfb.FilterCount - 1;

                p_0T = Convolution(inputSamplesI, inputSamplesQ, inc, pfb.taps[imu]);
                outputSamplesI[i] = (float)p_0T.Real;
                outputSamplesQ[i] = (float)p_0T.Imaginary;

                // Slice it
                c_0T = new Complex(p_0T.Real > 0.0f ? 1.0f : 0.0f, p_0T.Imaginary > 0.0f ? 1.0f : 0.0f);

                // Calc error
                phaseError = (float)(((p_0T - p_2T) * Complex.Conjugate(c_1T)) - ((c_0T - c_2T) * Complex.Conjugate(p_1T))).Real;
                phaseError = BranchlessClip(phaseError, 1);

                // Adjust omega
                omega = omega + omegaGain * phaseError;
                omega = omegaMid + BranchlessClip((omega - omegaMid), omegaLimit);

                // Adjust phase
                mu = mu + omega + muGain * phaseError;
                inc += (int)Floor(mu); /*if (mu > 1) { inc += (int)Floor(mu); }*/
                mu -= MathF.Floor(mu);

                if (inc < 0)
                    inc = 0;

                /*
                phase_error = (((p_0T - p_2T) * c_1T.conj()) - ((c_0T - c_2T) * p_1T.conj())).real;
                phase_error = BRANCHLESS_CLIP(phase_error, 1.0);

                //这里实现了一个PI控制环路，用于根据前面计算出的phase_error来自动补偿采样时间，omega_gain是环路的beta，mu_gain则是alpha
                //类似costas环，根据phase_error来在本地重建采样时钟
                //频率调整（积分项）,omega: 当前角频率（采样间隔的估计值）,omega_limit: 频率调整的最大范围
                // Adjust omega
                omega = omega + omega_gain * phase_error;
                omega = omega_mid + BRANCHLESS_CLIP((omega - omega_mid), omega_limit);  //根据初始化时传入的omega_limit的值来限制omega的大小，防止在snr低的情况下环路失控

                //相位调整（比例项+累积）,mu: 小数间隔（分数延迟），表示采样点与理想采样点之间的偏移,inc: 累积的采样间隔计数
                // Adjust phase
                mu = mu + omega + mu_gain * phase_error;
                inc += int(floor(mu)); //当mu累积超过1时，代表要对下一个Symbol进行采样，floor(mu)给出整数增量，加到inc
                mu -= floor(mu); //留小数部分在mu中用于下次迭代
                if (inc < 0)
                    inc = 0;
                */
            }
            //return p_0T;
        }

        /// <summary>
        /// Perform a convolution once through input samples.
        /// </summary>
        /// <param name="inputSamplesI">Input samples(Real part)</param>
        /// <param name="inputSamplesQ">Input samples(Imag part)</param>
        /// <param name="startIndex">Start index of input samples</param>
        /// <param name="pfbTaps">Taps for the convolution(form <see cref="PolyphaseFilterBank"/>)</param>
        /// <returns>Convolved sample</returns>
        public Complex Convolution(float[] inputSamplesI, float[] inputSamplesQ, int startIndex, double[] pfbTaps)
        {
            double sumI = 0;
            double sumQ = 0;

            for(int i = 0; i < pfbTaps.Length; i++)
            {
                sumI += inputSamplesI[i + startIndex] * pfbTaps[i];
                sumQ += inputSamplesQ[i + startIndex] * pfbTaps[i];
            }

            return new Complex(sumI, sumQ);
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
