using System;
using System.Collections.Generic;
using System.Numerics;
using NWaves.Filters.Base;

namespace RX_SSDV
{
    public class PolyphaseFilterBank
    {
        public FirFilter[] filters;
        public FirFilter[] multirateFilters;

        public PolyphaseFilterBank(double[] kernel, int n, int type = 1)
        {
            filters = new FirFilter[n];
            multirateFilters = new FirFilter[n];

            var len = (kernel.Length + 1) / n;

            for (var i = 0; i < filters.Length; i++)
            {
                var filterKernel = new double[kernel.Length];
                var mrFilterKernel = new double[len];

                for (var j = 0; j < len; j++)
                {
                    var kernelPos = i + n * j;

                    if (kernelPos < kernel.Length)
                    {
                        filterKernel[kernelPos] = kernel[kernelPos];
                        mrFilterKernel[j] = kernel[kernelPos];
                    }
                }

                filters[i] = new FirFilter(filterKernel);
                multirateFilters[i] = new FirFilter(mrFilterKernel);
            }

            // type-II -> reverse

            if (type == 2)
            {
                for (var i = 0; i < filters.Length / 2; i++)
                {
                    var tmp = filters[i];
                    filters[i] = filters[n - 1 - i];
                    filters[n - 1 - i] = tmp;

                    tmp = multirateFilters[i];
                    multirateFilters[i] = multirateFilters[n - 1 - i];
                    multirateFilters[n - 1 - i] = tmp;
                }
            }
        }
    }
}