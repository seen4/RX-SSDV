using NWaves.Filters.Base;
using RX_SSDV.IO;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;


namespace RX_SSDV.DSP
{
    public class LMS_DD_Equalizer : ComplexFirFilter
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
            SetHistory(SampleSource.WAV_BUFFER_SIZE * 2); //Buffer
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

        #region Buffer(DspBlock)
        public RingBufferIQ historyBuffer;

        /// <summary>
        /// Clear the buffer and allocate new one.
        /// </summary>
        /// <param name="bufferSize">New buffer size</param>
        private void SetHistory(int bufferSize)
        {
            historyBuffer = new RingBufferIQ(bufferSize);
        }

        private void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outSamplesI, float[] outSamplesQ, int inputSize)
        {
            if (inputSamplesI.Length != inputSamplesQ.Length)
            {
                throw new ArgumentException("inputSamplesI.Length must equals inputSamplesQ.Length");
            }
            if (outSamplesI.Length != outSamplesQ.Length)
            {
                throw new ArgumentException("outputSamplesI.Length must equals outputSamplesQ.Length");
            }

            historyBuffer.Write(inputSamplesI, inputSamplesQ, inputSize);
        }

        protected void CompleteProcess(int outputSize)
        {
            historyBuffer.MoveOutputIndex(outputSize);
        }
        #endregion

        /// <summary>
        /// Process samples.
        /// </summary>
        /// <param name="inputI">Input real samples.</param>
        /// <param name="inputQ">Input imag samples.</param>
        /// <param name="outputI">Output real samples.</param>
        /// <param name="outputQ">Output imag samples.</param>
        public int Process(int inputSize, float[] inputI, float[] inputQ, float[] outputI, float[] outputQ)
        {
            Process(inputI, inputQ, outputI, outputQ, inputSize);

            int outputSize = 0;
            for (int i = 0, j = 0; i < outputI.Length; i++, j+=samplesPerSymbol)
            {
                if(j >= historyBuffer.Length)
                {
                    break;
                }

                outputSize++;

                Complex inputSample = historyBuffer[j];
                (float, float) filterOutput = Process((float)inputSample.Real, (float)inputSample.Imaginary);

                float sampleI = outputI[i] = filterOutput.Item1;
                float sampleQ = outputQ[i] = filterOutput.Item2;

                Complex sample = new Complex(sampleI, sampleQ);

                Complex error = CalcError(sample);
                for (int k = 0; k < _kernelSize; k++)
                {
                    float inI = _delayLineI[k];
                    float inQ = _delayLineQ[k];
                    Complex input = new Complex(inI, inQ);
                    Complex deltaTap = CalcTap(input, error);
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

            CompleteProcess(outputSize);
            return outputSize;
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
            float decision = BpskDemod.BpskDecisionMaker(sample);
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