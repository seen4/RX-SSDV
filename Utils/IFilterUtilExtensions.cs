using NWaves.Filters.Base;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NWaves.Filters.Base
{
    /// <summary>
    /// Extends some filter classes of NWaves.
    /// </summary>
    public static class IFilterUtilExtensions
    {
        /// Origin: NWaves
        /// Modified 'FilterOnline' to make it use 'float[]' as input instead of 'DiscreteSignal'.
        /// <summary>
        /// Filters entire <paramref name="signal"/> by processing each signal sample in a loop.
        /// </summary>
        /// <param name="filter">Online filter</param>
        /// <param name="signal">Input signal</param>
        public static float[] FilterOnline(this IOnlineFilter filter, float[] signal)
        {
            var output = new float[signal.Length];
            var samples = signal;

            for (var i = 0; i < samples.Length; i++)
            {
                output[i] = filter.Process(samples[i]);
            }

            return output;
        }
    }
}
