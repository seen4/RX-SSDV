using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        /// <summary>
        /// Calculate the parity of an input number (mod 2 sum).
        /// </summary>
        /// <param name="input">Input integer</param>
        /// <returns>The mod 2 sum</returns>
        public static int Parity(int input)
        {
            input ^= input >> 16;
            input ^= input >> 8;
            input ^= input >> 4;
            input ^= input >> 2;
            input ^= input >> 1;
            return input & 1;
        }

        public static int HammingDst(int x, int y)
        {
            return HammingDst((uint)x, (uint)y);
        }

        public static int HammingDst(uint x, uint y)
        {
            uint xor = x ^ y;
            return BitOperations.PopCount(xor);
        }
    }
}
