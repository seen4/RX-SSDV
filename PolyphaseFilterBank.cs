using NWaves.Filters.Base;
using NWaves.Filters.Polyphase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RX_SSDV
{
    /// <summary>
    /// The complex version of <see cref="PolyphaseSystem"/>.(Constructor only)
    /// </summary>
    public class PolyphaseFilterBank
    {
        public ComplexFirFilter[] filters;
        public ComplexFirFilter[] multirateFilters;

        public PolyphaseFilterBank(Complex[] kernel, int n)
        {
            int len = (kernel.Length + 1) / n;

            filters = new ComplexFirFilter[n];
            multirateFilters = new ComplexFirFilter[n];

            for (int i = 0; i < filters.Length; i++)
            {
                float[] filterKernelI = new float[kernel.Length];
                float[] filterKernelQ = new float[kernel.Length];
                float[] mrFilterKernelI = new float[len];
                float[] mrFilterKernelQ = new float[len];

                for (var j = 0; j < len; j++)
                {
                    var kernelPos = i + n * j;

                    if (kernelPos < kernel.Length)
                    {
                        Complex currentKernel = kernel[kernelPos];
                        filterKernelI[kernelPos] = (float)currentKernel.Real;
                        filterKernelQ[kernelPos] = (float)currentKernel.Imaginary;
                        mrFilterKernelI[j] = (float)currentKernel.Real;
                        mrFilterKernelQ[j] = (float)currentKernel.Imaginary;
                    }
                }

                filters[i] = new ComplexFirFilter(filterKernelI, filterKernelQ);
                multirateFilters[i] = new ComplexFirFilter(mrFilterKernelI, mrFilterKernelQ);
            }
        }
    }
}
