using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Utils
{
    public class BinaryUtils
    {
        /// <summary>
        /// Read the binary value of an integer at a specific position.
        /// </summary>
        /// <param name="input">Input value</param>
        /// <param name="pos">Specific Position</param>
        /// <returns>The binary value</returns>
        public static byte ReadInt(int input, int pos)
        {
            //eg.
            //input=1101_0100 index=2
            //targetNum=0000_0100
            //input & targetNum = 0000_0100 == targetNum


            //if(index < 1)
            //    throw new ArgumentOutOfRangeException("index must bigger than zero.");

            //int targetNum = 1 << (index - 1);
            //if((input & targetNum) == targetNum)
            //    return 1;
            //else
            //    return 0;


            //Easier method
            return (byte)((input >> (pos - 1)) & 1);
        }
    }
}
