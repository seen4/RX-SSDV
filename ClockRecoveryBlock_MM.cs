using NWaves.Filters.Base;
using NWaves.Utils;
using NWaves.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static RX_SSDV.CostasLoop;
using static System.Math;
using static RX_SSDV.Utils.FilterUtils;

namespace RX_SSDV
{
    public class ClockRecoveryBlock_MM : DspBlock
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

        //DEBUG
        public float Mu => mu;
        public float Omega => omega;

        public ClockRecoveryBlock_MM(float mu, float muGain, float omega, float omegaGain, float omegaLimit) : base()
        {
            this.mu = mu;
            this.muGain = muGain;
            this.omega = omega;
            this.omegaGain = omegaGain;
            omegaRelativeLimit = omegaLimit;

            omegaMid = omega;
            omegaLimit = omegaRelativeLimit * omega;

            //UpdatePFB(nFilter, nTaps);
        }

        /*
        public void UpdatePFB(int nFilt, int nTaps)
        {
            //pfb = new PolyphaseFilterBank(RootRaisedCosine(16, nFilt * nTaps, 1, 0.35, nTaps), nFilt);
            pfb = new PolyphaseFilterBank(WindowedSinc(nFilt * 128, Math.PI / nFilt, nFilt), nFilt);
        }
        */

        /// <summary>
        /// Process signal.
        /// </summary>
        /// <param name="inputSamplesI">I channel of input samples(Real part)</param>
        /// <param name="inputSamplesQ">Q channel of input samples(Imag part)</param>
        /// <param name="outputSamplesI">I channel of output samples(Real part)</param>
        /// <param name="outputSamplesQ">Q channel of output samples(Imag part)</param>
        /// <returns>Output samples array size</returns>
        public override int Process(int inputSize, float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {
            base.Process(inputSamplesI, inputSamplesQ, outputSamplesI, outputSamplesQ, inputSize);

            if (inputSamplesI.Length != inputSamplesQ.Length)
                throw new ArgumentException("inputSamplesI.Length mush equals inputSamplesQ.Length");

            // See Satdump:clock_recovery_mm.cpp

            ouc = 0;
            inc = 0;

            for (; ouc < outputSamplesI.Length && inc < inputSize;)
            {
                if (inc + nTapsInterpolator >= historyBuffer.Length)
                    break;

                // Propagate delay
                p_2T = p_1T;
                p_1T = p_0T;
                c_2T = c_1T;
                c_1T = c_0T;

                // Compute output
                int imu = (int)MathF.Round(mu * nFiltInterpolator);
                if (imu < 0) // If we're out of bounds, clamp
                    imu = 0;
                if (imu >= nFiltInterpolator)
                    imu = nFiltInterpolator - 1;

                p_0T = DotProd(inc, interpolatorTaps[^(imu + 1)]);
                outputSamplesI[ouc] = (float)p_0T.Real;
                outputSamplesQ[ouc++] = (float)p_0T.Imaginary;

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

            CompleteProcess(inc);
            return ouc;
        }

        /// <summary>
        /// Perform a dot produce once through input samples.
        /// </summary>
        /// <param name="startIndex">Start index of input samples</param>
        /// <param name="pfbTaps">Taps for the convolution(form <see cref="PolyphaseFilterBank"/>)</param>
        /// <returns>Dot produce</returns>
        protected Complex DotProd(int startIndex, double[] pfbTaps)
        {
            double sumI = 0;
            double sumQ = 0;

            for (int i = 0; i < pfbTaps.Length; i++)
            {
                Complex sample = historyBuffer[i + startIndex];
                float sampleI = (float)sample.Real;
                float sampleQ = (float)sample.Imaginary;

                sumI += sampleI * pfbTaps[i];
                sumQ += sampleQ * pfbTaps[i];
            }

            return new Complex(sumI, sumQ);
        }

        /// <summary>
        /// Calcucate the size of the clock recovery output array(May equals the real output plus one).
        /// </summary>
        /// <param name="inputSize">Input array size</param>
        /// <returns>Output array size</returns>
        public int CalcOutputSize(int inputSize)
        {
            return (int)((float)inputSize / omega) + 1;
        }

        /*
        private double[] WindowedSinc(int nTaps, double alpha, double norm)
        {
            double[] resampTaps = new double[nTaps];
            double half = nTaps / 2;
            double corr = norm * omega / PI;
            for(int i = 0; i < nTaps; i++)
            {
                double t = i - half;
                resampTaps[i] = Sinc(t * alpha) * corr;
            }
            resampTaps.ApplyWindow(WindowType.Blackman);
            return resampTaps;
        }

        private double Sinc(double x)
        {
            return x == 0 ? 1 : Sin(x) / x;
        }
        */
    }
}
