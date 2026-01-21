using NWaves.Filters.Base;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;


namespace RX_SSDV
{
    public class LMS_DD_Equalizer : ComplexFirFilter, IDspBlock
    {
        private float mu;
        public float Gain => mu;
        private int samplesPerSymbol;
        public int SamplesPerSymbol
        {
            //目前想不到加什么限制
            get
            {
                return samplesPerSymbol;
            }
            set
            {
                samplesPerSymbol = value;
            }
        }

        public LMS_DD_Equalizer(IEnumerable<float> kernelI, IEnumerable<float> kernelQ, float gain, int samplesPerSymbol) : base(kernelI, kernelQ)
        {
            SetGain(gain);
            this.samplesPerSymbol = samplesPerSymbol;
        }

        public static LMS_DD_Equalizer BuildEqualizer(float gain, int kernelSize, int samplesPerSymbol)
        {
            float[] kernelI = new float[kernelSize];
            float[] kernelQ = new float[kernelSize];

            kernelI[0] = 1;

            LMS_DD_Equalizer equalizer = new LMS_DD_Equalizer(kernelI, kernelQ, gain, samplesPerSymbol);
            equalizer.SetGain(gain);
            return equalizer;
        }

        /// <summary>
        /// Process samples.
        /// </summary>
        /// <param name="inputI">Input real samples.</param>
        /// <param name="inputQ">Input imag samples.</param>
        /// <param name="outputI">Output real samples.</param>
        /// <param name="outputQ">Output imag samples.</param>
        public void Process(float[] inputI, float[] inputQ, float[] outputI, float[] outputQ)
        {
            for(int i = 0, j = 0; i < outputI.Length; i++, j += samplesPerSymbol)
            {
                if(j >= inputI.Length)
                {
                    break;
                }
                (float, float) filterOutput = Process(inputI[j], inputQ[j]);

                float sampleI = outputI[i] = filterOutput.Item1;
                float sampleQ = outputQ[i] = filterOutput.Item2;

                Complex inputSample = new Complex(inputI[j], inputQ[j]);
                Complex sample = new Complex(sampleI, sampleQ);

                Complex error = CalcError(sample);
                for (int k = 0; k < _kernelSize; k++)
                {
                    Complex deltaTap = CalcTap(inputSample, error);
                    float deltaI = (float)deltaTap.Real;
                    float deltaQ = (float)deltaTap.Imaginary;
                    _bI[k] += deltaI;
                    _bI[_kernelSize + k] += deltaI;
                    _bQ[k] += deltaQ;
                    _bQ[_kernelSize + k] += deltaQ;

                    //if (i > 189)
                    //{
                    //    int a = 0;
                    //}
                }
            }
        }

        private Complex CalcTap(Complex input, Complex d_error)
        {
            return mu * Complex.Conjugate(input) * d_error;
        }

        /// <summary>
        /// Calculates error of <see cref="LMS_DD_Equalizer"/> with decision.
        /// </summary>
        /// <param name="sample">Input sample</param>
        /// <returns>Calculated error.</returns>
        public Complex CalcError(Complex sample)
        {
            float decision = BPSKDemod.BpskDecisionMaker(sample);
            Complex error = new Complex(decision - sample.Real, -sample.Imaginary);
            return error;
        }

        /// <summary>
        /// Set gain.
        /// </summary>
        /// <param name="gain">New gain</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if gain value not in [0, 1]</exception>
        public void SetGain(float gain)
        {
            if(gain < 0 || gain > 1)
            {
                throw new ArgumentOutOfRangeException("gain value must in [0, 1]");
            }
            else
            {
                mu = gain;
            }
        }
    }
}