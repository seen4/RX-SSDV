using NWaves.Utils;
using NWaves.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NWaves.Filters.Fda.DesignFilter;

namespace RX_SSDV.Utils
{
    /// <summary>
    /// Origin: NWaves.Filters.Fda.DesignFilter
    /// </summary>
    public static class DesignFilterUtil
    {
        /// <summary>
        /// Designs ideal bandpass fractional-delay FIR filter using sinc-window method.(Real)(No freq gurad)
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="frequencyLow">Normalized low cutoff frequency in range [0..0.5]</param>
        /// <param name="frequencyHigh">Normalized high cutoff frequency in range [0..0.5]</param>
        /// <param name="delay">Fractional delay</param>
        /// <param name="window">Window</param>
        public static double[] FirWinFdBpReal(int order, double frequencyLow, double frequencyHigh, double delay, WindowType window = WindowType.Blackman)
        {
            //Guard.AgainstInvalidRange(frequencyLow, 0, 0.5, "low cutoff frequency");
            //Guard.AgainstInvalidRange(frequencyHigh, 0, 0.5, "high cutoff frequency");
            //Guard.AgainstInvalidRange(frequencyLow, frequencyHigh, "low cutoff frequency", "high cutoff frequency");

            var kernel = new double[order];

            var middle = (order - 1) / 2;
            var freq12Pi = 2 * Math.PI * frequencyLow;
            var freq22Pi = 2 * Math.PI * frequencyHigh;

            for (var i = 0; i < order; i++)
            {
                var d = i - delay - middle;

                kernel[i] = d == 0 ? 2 * (frequencyHigh - frequencyLow) : (Math.Sin(freq22Pi * d) - Math.Sin(freq12Pi * d)) / (Math.PI * d);
            }

            kernel.ApplyWindow(window);

            NormalizeKernel(kernel, 2 * Math.PI * (frequencyLow + frequencyHigh) / 2);

            return kernel;
        }

        /// <summary>
        /// Designs ideal bandpass fractional-delay FIR filter using sinc-window method.(Imag)(No freq gurad)
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="frequencyLow">Normalized low cutoff frequency in range [0..0.5]</param>
        /// <param name="frequencyHigh">Normalized high cutoff frequency in range [0..0.5]</param>
        /// <param name="delay">Fractional delay</param>
        /// <param name="window">Window</param>
        public static double[] FirWinFdBpImag(int order, double frequencyLow, double frequencyHigh, double delay, WindowType window = WindowType.Blackman)
        {
            //Guard.AgainstInvalidRange(frequencyLow, 0, 0.5, "low cutoff frequency");
            //Guard.AgainstInvalidRange(frequencyHigh, 0, 0.5, "high cutoff frequency");
            //Guard.AgainstInvalidRange(frequencyLow, frequencyHigh, "low cutoff frequency", "high cutoff frequency");

            var kernel = new double[order];

            var middle = (order - 1) / 2;
            var freq12Pi = 2 * Math.PI * frequencyLow;
            var freq22Pi = 2 * Math.PI * frequencyHigh;

            for (var i = 0; i < order; i++)
            {
                var d = i - delay - middle;

                kernel[i] = d == 0 ? 0 * 2 * (frequencyHigh - frequencyLow) : (Math.Cos(freq22Pi * d) - Math.Cos(freq12Pi * d)) / (Math.PI * d);
            }

            kernel.ApplyWindow(window);

            NormalizeKernel(kernel, 2 * Math.PI * (frequencyLow + frequencyHigh) / 2);

            return kernel;
        }

        /// <summary>
        /// Designs ideal bandpass FIR filter using sinc-window method.(Real)
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="frequencyLow">Normalized low cutoff frequency in range [0..0.5]</param>
        /// <param name="frequencyHigh">Normalized high cutoff frequency in range [0..0.5]</param>
        /// <param name="window">Window</param>
        public static double[] FirWinBpReal(int order, double frequencyLow, double frequencyHigh, WindowType window = WindowType.Blackman)
        {
            return FirWinFdBpReal(order, frequencyLow, frequencyHigh, (order + 1) % 2 * 0.5, window);
        }

        /// <summary>
        /// Designs ideal bandpass FIR filter using sinc-window method.(Imag)
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="frequencyLow">Normalized low cutoff frequency in range [0..0.5]</param>
        /// <param name="frequencyHigh">Normalized high cutoff frequency in range [0..0.5]</param>
        /// <param name="window">Window</param>
        public static double[] FirWinBpImag(int order, double frequencyLow, double frequencyHigh, WindowType window = WindowType.Blackman)
        {
            return FirWinFdBpImag(order, frequencyLow, frequencyHigh, (order + 1) % 2 * 0.5, window);
        }
    }
}
