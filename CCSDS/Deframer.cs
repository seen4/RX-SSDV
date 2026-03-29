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
        public override int Process(int inputSize, byte[] inputArr, byte[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);

            //Output bits(Secondary)
            if (isPackingBits)
            {
                int remainLength = packetSize - packetBitsInc;
                WritePacketBuffer(0, Math.Min(remainLength, historyBuffer.Length));
                if (remainLength <= historyBuffer.Length)
                {
                    isPackingBits = false;
                    packetBitsInc = 0;
                    SendBits();
                }
                else
                {
                    int count = historyBuffer.Length;
                    CompleteProcess(count);
                    return count;
                }
            }

            int processedCount = 0;
            for (int i = 0; i + syncwordSize <= historyBuffer.Length; i++)
            {
                processedCount++;

                uint window = 0;
                for (int j = 0; j < syncwordSize; j++)
                {
                    window <<= 1;
                    window |= (byte)(historyBuffer[i + j] & 0b_01);
                }

                if ((BinaryUtils.HammingDst(CCSDS_ASM, window) <= 2 || BinaryUtils.HammingDst(CCSDS_ASM, ~window) <= 2) && syncwordSize == 32)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced ASM at {i}, {window.ToString("B32")}");

                    // Output bits(Primary)
                    int packetIndex = i + syncwordSize;
                    int remainLength = historyBuffer.Length - packetIndex;
                    WritePacketBuffer(packetIndex, Math.Min(remainLength, packetSize));
                    if (remainLength < packetSize)
                        isPackingBits = true;
                    else
                    {
                        packetBitsInc = 0;
                        SendBits();
                    }

                    //Stop scanning to avoid incorrect output;
                    int count = historyBuffer.Length;
                    CompleteProcess(count);
                    return count;
                }
                //else if ((ssdvSyncSymbol == window || ssdvSyncSymbol == ~window) && syncSymbolSize == 12)
                //{
                //    Logger.CLogInfo($"[FrameSync-Debug]Synced SSDV at {i}");
                //}
            }

            CompleteProcess(processedCount);
            return processedCount;
        }

        private void WritePacketBuffer(int index, int length)
        {
            for (int i = 0; i < length; i++)
            {
                packetBits[packetBitsInc++] = (byte)historyBuffer[index + i];
            }
        }

        private void SendBits()
        {
            //TODO: data process
            //The bit data of current packet was stored in 'packetBits' array
            Logger.CLogInfo($"[Packet RX] Packet received, length = { packetSize }bits({ packetSize / 8 }bytes)");
            //Logger.CPrintArr(packetBits, packetBits.Length, "Packet data");
            onPacketProcess(packetBits);
        }
    }
}
