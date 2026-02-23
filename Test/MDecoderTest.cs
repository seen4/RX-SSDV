using RX_SSDV.Base;
using RX_SSDV.CCSDS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Test
{
    public class MDecoderTest
    {
        public static void Test()
        {
            MDecoder decoder = new MDecoder();

            float[] inputArr = 
            { 
                1, 1, 1, 0, 
                0, 1, 0, 1, 
                0, 0, 1, 1, 
                0, 1, 0, 1, 
                1, 1, 1, 0, 
                0, 1, 0, 1, 
                0, 0, 1, 1, 
                0, 1, 0, 1 
            };

            float[] outputArr = new float[inputArr.Length];

            decoder.Process(inputArr.Length, inputArr, outputArr);

            Logger.PrintArr(outputArr, outputArr.Length, "Test M out");
        }
    }
}
