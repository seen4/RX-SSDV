using Microsoft.VisualBasic.ApplicationServices;
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
    public class Deframer : DigitalProcessingBlock
    {
        //Symbol sync
        public const uint CCSDS_ASM = 0x1ACFFC1D; //0b_0001_1010_1100_1111_1111_1100_0001_1101 1ACFFC1D | inverse 0b_1011_1000_0011_1111_1111_0011_0101_1000 B83FF358
        //public const uint ssdvSyncSymbol = 0x0322; //0b_0011_0010_0010
        public const int syncwordSize = 32;
        private bool isSynced = false;

        private int packetSize;
        private byte[] packetBits;
        private int packetBitsInc = 0;
        private bool isPackingBits = false;

        public int PacketSize
        {
            get { return packetSize; } 
            set { packetSize = value; packetBits = new byte[packetSize]; }
        }

        public Action<byte[]> onPacketProcess = (data) => { };

        public Deframer(int packetSize) 
        {
            this.packetSize = packetSize;
            packetBits = new byte[packetSize];
        }

        //This module have no stream output
        public override int Process(int inputSize, float[] inputArr, float[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);

            int processedCount = 0;
            for (int i = 0; i + syncwordSize <= historyBuffer.Length; i++)
            {
                processedCount++;

                if (isPackingBits)
                {
                    int remainBits = packetSize - packetBitsInc;
                    if (remainBits > historyBuffer.Length)
                    {
                        for (int j = 0; j < historyBuffer.Length; j++)
                        {
                            packetBits[packetBitsInc++] = (byte)historyBuffer[j];
                        }
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < remainBits; j++)
                        {
                            packetBits[packetBitsInc++] = (byte)historyBuffer[j];
                        }
                        isPackingBits = false;
                        packetBitsInc = 0;
                        SendBits();
                    }
                }

                uint window = 0;
                for (int j = 0; j < syncwordSize; j++)
                {
                    window <<= 1;
                    window |= (byte)((int)historyBuffer[i + j] & 0b_01);
                }

                //Logger.CLogInfo($"[FrameSync-Debug] {window.ToString("B32")}");

                if ((BinaryUtils.HammingDst(CCSDS_ASM, window) <= 2 || BinaryUtils.HammingDst(CCSDS_ASM, ~window) <= 2) && syncwordSize == 32)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced ASM at {i}, {window.ToString("B32")}");
                    if (i + syncwordSize + packetSize <= historyBuffer.Length)
                    {
                        for (int k = 0; k < packetSize; k++)
                        {
                            packetBits[k] = (byte)historyBuffer[i + syncwordSize + k];
                        }
                        SendBits();
                    }
                    else
                    {
                        isPackingBits = true;
                        int remainLength = historyBuffer.Length - i - syncwordSize;
                        for (int k = 0; k < remainLength; k++)
                        {
                            packetBits[packetBitsInc++] = (byte)historyBuffer[i + syncwordSize + k];
                        }
                    }
                }
                //else if ((ssdvSyncSymbol == window || ssdvSyncSymbol == ~window) && syncSymbolSize == 12)
                //{
                //    Logger.CLogInfo($"[FrameSync-Debug]Synced SSDV at {i}");
                //}
            }

            CompleteProcess(processedCount);
            return processedCount;
        }

        private void SendBits()
        {
            //TODO: data process
            //The bit data of current packet was stored in 'packetBits' array
            Logger.CLogInfo($"[Packet RX] Packet received, length = { packetSize }");
            //Logger.CPrintArr(packetBits, packetBits.Length, "Packet data");
            onPacketProcess(packetBits);
        }
    }
}
