using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Utils
{
    public class ArrayUtil
    {
        public static float[] Double2Float(double[] array)
        {
            float[] output = new float[array.Length];

            for(int i = 0; i < array.Length; i++)
            {
                output[i] = (float)array[i];
            }

            return output;
        }
    }
}
