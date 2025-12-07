using NWaves.Filters.Base;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV
{
    public class ComplexFirFilter
    {
        private FirFilter firFilterI;
        private FirFilter firFilterQ;

        public ComplexFirFilter(double[] kernelI, double[] kernelQ)
        {
            firFilterI = new FirFilter(kernelI);
            firFilterQ = new FirFilter(kernelQ);
        }

        public ComplexFirFilter(float[] kernelI, float[] kernelQ)
        {
            firFilterI = new FirFilter(kernelI);
            firFilterQ = new FirFilter(kernelQ);
        }

        /// <summary>
        /// Process complex signal.
        /// </summary>
        /// <param name="realSignal">Real signal</param>
        /// <param name="imagSignal">Imag signal</param>
        /// <returns>Filtered signal (outReal, outImag)</returns>
        public (float[], float[]) Process(float[] realSignal, float[] imagSignal)
        {
            float[] outI = firFilterI.FilterOnline(realSignal);
            float[] outQ = firFilterQ.FilterOnline(imagSignal);

            float[] outReal = new float[outI.Length];
            float[] outImag = new float[outQ.Length];

            for(int i = 0; i < outI.Length; i++)
            {
                outReal[i] = outQ[i] - outI[i];
                outImag[i] = outQ[i] + outI[i];
            }

            return (outReal, outImag);
        }

        public void ChangeKernel(float[] kernelReal, float[] kernelImag)
        {
            firFilterI.ChangeKernel(kernelReal);
            firFilterQ.ChangeKernel(kernelImag);
        }

        public void ChangeKernel(double[] kernelReal, double[] kernelImag)
        {
            firFilterI.ChangeKernel(ArrayUtil.Double2Float(kernelReal));
            firFilterQ.ChangeKernel(ArrayUtil.Double2Float(kernelImag));
        }
    }
}
