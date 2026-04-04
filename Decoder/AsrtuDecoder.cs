using RX_SSDV.Base;
using RX_SSDV.IO;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Decoder
{
    public class AsrtuDecoder : ITransportDecoder
    {
        public ByteOutput byteOutput;
        public byte[] dataBuffer;

        public readonly string resDirPath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs";
        public readonly string binaryFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs/ssdv-packets.bin";
        public readonly string imageFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs/output-image.jpg";
        public readonly string ssdvDecoderPath = $"{AppDomain.CurrentDomain.BaseDirectory}ssdv.exe";

        Process process;
        ProcessStartInfo startInfo;

        private byte[] ssdvStandardHeader =
        {
            0x55, 0x66, 0xDA, 0x4B, 0xF8, 0xEF, 0x00
        };

        public AsrtuDecoder()
        {
            byteOutput = new ByteOutput(binaryFilePath);
            dataBuffer = new byte[256];

            process = new Process();
            startInfo = new ProcessStartInfo(ssdvDecoderPath, $"-d {binaryFilePath} {imageFilePath}"); // ssdv.exe -d ssdv-packets.bin output-image.jpg
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;

            SampleSource.onStop += () => byteOutput.CloseStream();
            SampleSource.onStart += () => byteOutput.OpenStream();

            CheckOutputFiles();
        }

        public void ProcessPacket(byte[] packet)
        {
            packet.FastCopyTo(dataBuffer, packet.Length, 0, 1);

            if (packet[0] == 0x03)
            {
                if (packet[1] == 0x22) // SSDV packet
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1]SSDV packet received.");

                    // Replace with standard SSDV header
                    ReplaceSSDVHeader();

                    // If we found the packet that its id equals 0x00, it must be the first packet.
                    if (dataBuffer[8] == 0x00)
                    {
                        byteOutput.ClearFile();
                        //byteOutput.fileStream.Seek(0, SeekOrigin.Begin);
                    }
                    
                    // Calculate CRC32
                    uint crc = CalcCRC32(dataBuffer);

                    // crc uint => 4 bytes, write to buffer
                    byte crcByte = 0;
                    for(int i = 0; i < 32; i++)
                    {
                        crcByte <<= 1;
                        crcByte |= (byte)((crc >> (31 - i)) & 1);
                        if((i + 1) % 8 == 0)
                        {
                            dataBuffer[219 + (i + 1) / 8] = crcByte;
                            crcByte = 0;
                        }
                    }
                    
                    byteOutput.WriteBytes(dataBuffer); // Write file
                    DecodeSSDV();
                }
                else if (packet[1] == 0x24) //Telemetry packet
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1]Telemetry packet received.");
                }
            }
        }

        private uint CalcCRC32(byte[] buffer)
        {
            uint result = 0xFFFFFFFF;
            //忽略开头的0x55
            for (int i = 1; i < 220; i++)
            {
                result ^= (uint)buffer[i] & 0xFF;

                for (int j = 0; j < 8; j++)
                {
                    if ((result & 0x1) == 1)
                    {
                        result >>= 1;
                        result &= 0x7FFFFFFF;//清除最高位
                        result ^= 0xEDB88320;
                    }
                    else
                    {
                        result >>= 1;
                        result &= 0x7FFFFFFF;//清除最高位
                    }
                }
            }
            return ~result;
        }

        private void ReplaceSSDVHeader()
        {
            for (int i = 0; i < ssdvStandardHeader.Length; i++)
            {
                dataBuffer[i] = ssdvStandardHeader[i];
            }
        }

        private void CheckOutputFiles()
        {
            if(!Directory.Exists(resDirPath))
                Directory.CreateDirectory(resDirPath);
            if(!File.Exists(binaryFilePath))
                File.Create(binaryFilePath);
            if(!File.Exists(imageFilePath))
                File.Create(imageFilePath);
        }

        private void DecodeSSDV()
        {
            if (!File.Exists(ssdvDecoderPath))
            {
                Logger.LogErr($"Decode SSDV failed, could not found decoder: '{ssdvDecoderPath}'");
                return;
            }
            StartDecoderProcess();
        }

        public bool StartDecoderProcess()
        {
            process.Start();
            //Task.Run(() => {
            //    process.WaitForExit();
            //    Logger.CLogInfo("[ssdv.exe]Standard output:\n" + process.StandardOutput.ReadToEnd());
            //});
            return true;
        }
    }
}
