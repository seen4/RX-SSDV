using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using NWaves.Operations.Convolution;
using NWaves.Signals;
using NWaves.Utils;

namespace NWaves.Filters.Base
{
    /// <summary>
    /// FirFilter for complex input, Referenced and modified several methods of <see cref="NWaves.Filters.Base.FirFilter"/>.
    /// </summary>
    public class ComplexFirFilter
    {
        /// <summary>
        /// Gets copy of the filter kernel I (impulse response).
        /// </summary>
        public float[] KernelI => _bI.Take(_kernelSize).ToArray();

        /// <summary>
        /// Gets copy of the filter kernel Q (impulse response).
        /// </summary>
        public float[] KernelQ => _bQ.Take(_kernelSize).ToArray();

        /// <summary>
        /// Numerator part coefficients in filter's transfer function 
        /// (non-recursive part in difference equations).
        /// </summary>
        protected readonly float[] _bI;

        /// <summary>
        /// Numerator part coefficients in filter's transfer function 
        /// (non-recursive part in difference equations).
        /// </summary>
        protected readonly float[] _bQ;

        // 
        // Since the number of coefficients can be really big,
        // we store only float versions and they are used for filtering.
        // 
        // Note.
        // This array is created from duplicated filter kernel:
        // 
        //   kernel                _b
        // [1 2 3 4 5] -> [1 2 3 4 5 1 2 3 4 5]
        // 
        // Such memory layout leads to significant speed-up of online filtering.
        //

        /// <summary>
        /// Kernel length.
        /// </summary>
        protected int _kernelSize;

        /// <summary>
        /// Internal buffer for delay line.(I)
        /// </summary>
        protected float[] _delayLineI;

        /// <summary>
        /// Current offset in delay line.(I)
        /// </summary>
        protected int _delayLineOffsetI;

        /// <summary>
        /// Internal buffer for delay line.(Q)
        /// </summary>
        protected float[] _delayLineQ;

        /// <summary>
        /// Current offset in delay line.(Q)
        /// </summary>
        protected int _delayLineOffsetQ;

        /// <summary>
        /// Constructs <see cref="FirFilter"/> from <paramref name="kernel"/>.
        /// </summary>
        /// <param name="kernelI">FIR filter kernel of channel I</param>
        /// <param name="kernelQ">FIR filter kernel of channel Q</param>
        public ComplexFirFilter(IEnumerable<float> kernelI, IEnumerable<float> kernelQ)
        {
            if (kernelI.Count() != kernelQ.Count())
            {
                throw new ArgumentException("The size of 'kernelI' must equals the size of 'kernelQ'.");
            }

            _kernelSize = kernelI.Count();

            _bI = new float[_kernelSize * 2];
            _bQ = new float[_kernelSize * 2];

            //iq通道滤波器参数(kernel)
            for (var i = 0; i < _kernelSize; i++)
            {
                _bI[i] = _bI[_kernelSize + i] = kernelI.ElementAt(i);
                _bQ[i] = _bQ[_kernelSize + i] = kernelQ.ElementAt(i);
            }

            _delayLineI = new float[_kernelSize];
            _delayLineOffsetI = _kernelSize - 1;

            _delayLineQ = new float[_kernelSize];
            _delayLineOffsetQ = _kernelSize - 1;
        }

        /// <summary>
        /// Processes one complex sample.
        /// </summary>
        /// <param name="sampleI">Input I sample of signal</param>
        /// <param name="sampleQ">Input Q sample of signal</param>
        public (float, float) Process(float sampleI, float sampleQ)
        {
            //卷积
            _delayLineI[_delayLineOffsetI] = sampleI;
            _delayLineQ[_delayLineOffsetQ] = sampleQ;

            var outputI = 0f;
            var outputQ = 0f;

            //修改: 复数乘法计算卷积，原实数算法请见NWaves.Filters.Base.FirFilter
            for (int i = 0, j = _kernelSize - _delayLineOffsetI; i < _kernelSize; i++, j++)
            {
                outputI += _delayLineI[i] * _bI[j] - _delayLineQ[i] * _bQ[j];
            }

            for (int i = 0, j = _kernelSize - _delayLineOffsetQ; i < _kernelSize; i++, j++)
            {
                outputQ += _delayLineI[i] * _bQ[j] + _delayLineQ[i] * _bI[j];
            }

            if (--_delayLineOffsetI < 0)
            {
                _delayLineOffsetI = _kernelSize - 1;
            }

            if (--_delayLineOffsetQ < 0)
            {
                _delayLineOffsetQ = _kernelSize - 1;
            }

            return (outputI, outputQ);
        }

        /// <summary>
        /// Processes one complex sample.
        /// </summary>
        /// <param name="sampleI">Input I sample of signal</param>
        /// <param name="sampleQ">Input Q sample of signal</param>
        public Complex ProcessComplex(float sampleI, float sampleQ)
        {
            (float, float) output = Process(sampleI, sampleQ);
            return new Complex(output.Item1, output.Item2);
        }

        /// <summary>
        /// Filters data frame-wise.
        /// </summary>
        /// <param name="inputI">Input block I of samples</param>
        /// <param name="outputI">Block I of filtered samples</param>
        /// <param name="inputQ">Input block I of samples</param>
        /// <param name="outputQ">Block I of filtered samples</param>
        /// <param name="count">Number of samples to filter</param>
        /// <param name="inputPos">Input starting index</param>
        /// <param name="outputPos">Output starting index</param>
        public void ProcessOnline(float[] inputI, float[] inputQ, float[] outputI, float[] outputQ, int count = 0, int inputPos = 0, int outputPos = 0)
        {
            if (inputI.Length != inputQ.Length)
                return;

            if (count <= 0)
            {
                count = inputI.Length;
            }

            var endPos = inputPos + count;

            for (int n = inputPos, m = outputPos; n < endPos; n++, m++)
            {
                (outputI[m], outputQ[m]) = Process(inputI[n], inputQ[n]);
            }
        }

        /// <summary>
        /// Changes filter kernel online.
        /// </summary>
        /// <param name="kernelI">New kernel I</param>
        /// <param name="kernelQ">New kernel Q</param>
        public void ChangeKernel(float[] kernelI, float[] kernelQ)
        {
            if (kernelI.Length != _kernelSize || kernelQ.Length != _kernelSize) return;

            for (var i = 0; i < _kernelSize; i++)
            {
                _bI[i] = _bI[_kernelSize + i] = kernelI[i];
                _bQ[i] = _bQ[_kernelSize + i] = kernelQ[i];
            }
        }

        /// <summary>
        /// Resets filter.
        /// </summary>
        public void Reset()
        {
            _delayLineOffsetI = _delayLineOffsetQ = _kernelSize - 1;
            Array.Clear(_delayLineI, 0, _kernelSize);
            Array.Clear(_delayLineQ, 0, _kernelSize);
        }
    }
}