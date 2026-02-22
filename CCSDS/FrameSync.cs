using RX_SSDV.Base;
using RX_SSDV.Utils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.CCSDS
{
    public class FrameSync : DigitalProcessingBlock
    {
        public const uint CCSDS_ASM = 0x1ACFFC1D; //0b_0001_1010_1100_1111_1111_1100_0001_1101 1ACFFC1D | invert 0b_0001_1010_1100_1111_1111_1100_0001_1101 B83FF358
        public const uint ssdvSyncSymbol = 0x0322; //0b_0011_0010_0010
        public const int syncSymbolSize = 32;

        public FrameSync() { }

        public override int Process(int inputSize, float[] inputArr, float[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);

            int processedCount = 0;
            for (int i = 0; i + syncSymbolSize <= historyBuffer.Length; i++)
            {
                processedCount++;

                uint window = 0;
                for (int j = 0; j < syncSymbolSize; j++)
                {
                    window <<= 1;
                    window |= (byte)((int)historyBuffer[i + j] & 0b_01);
                }

                //Logger.CLogInfo("[FrameSync-Debug]" + Convert.ToString(window, 2));

                if ((BinaryUtils.HammingDst(CCSDS_ASM, window) <= 1 || BinaryUtils.HammingDst(CCSDS_ASM, ~window) <= 1) && syncSymbolSize == 32)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced ASM at {i}");
                }
                else if ((ssdvSyncSymbol == window || ssdvSyncSymbol == ~window) && syncSymbolSize == 12)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced SSDV at {i}");
                }
            }

            CompleteProcess(processedCount);
            return 0;
        }
    }
}
