using RX_SSDV.Base;
using RX_SSDV.IO;
using RX_SSDV.UI;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

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

        Timer? decoderTimer;

        public AsrtuDecoder()
        {
            byteOutput = new ByteOutput(binaryFilePath);
            dataBuffer = new byte[256];

            process = new Process();
            startInfo = new ProcessStartInfo(ssdvDecoderPath, $"-d {binaryFilePath} {imageFilePath}"); // ssdv.exe -d ssdv-packets.bin output-image.jpg
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;

            SampleSource.onStop += () => { byteOutput.CloseStream(); };
            SampleSource.onStart += () => { byteOutput.OpenStream(); CheckOutputFiles(); };

            decoderTimer = new Timer(1000);
            decoderTimer.Elapsed += (object? e, ElapsedEventArgs args) => { 
                decoderTimer.Close(); 
                decoderTimer.Dispose(); 
                decoderTimer = null; 
                DecodeSSDV();

                Logger.LogInfo("[ASTRU-1] Processing SSDV packets...");
            };

            CheckOutputFiles();
        }

        private void ResetTimer()
        {
            decoderTimer = new Timer(1000);
            decoderTimer.Elapsed += (object? e, ElapsedEventArgs args) => { 
                decoderTimer.Close(); 
                decoderTimer.Dispose(); 
                decoderTimer = null; 
                DecodeSSDV();

                Logger.LogInfo("[ASTRU-1] Processing SSDV packets...");
            };
        }

        public void ProcessPacket(byte[] packet)
        {
            packet.FastCopyTo(dataBuffer, packet.Length, 0, 1);

            if (packet[0] == 0x03)
            {
                if (packet[1] == 0x22) // SSDV packet
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1] SSDV packet received.");

                    // Restart timer
                    if (decoderTimer == null)
                    {
                        ResetTimer();
                    }
                    else
                    {
                        decoderTimer.Stop();
                        decoderTimer.Start();
                    }

                    // Replace with standard SSDV header
                    ReplaceSSDVHeader();

                    // If we found the packet that its id equals 0x00, it must be the first packet.
                    if (dataBuffer[8] == 0x00)
                    {
                        byteOutput.ClearFile();
                        PostPacket(PacketData.PacketType.Image);
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
                    //DecodeSSDV();
                }
                else if (packet[1] == 0x24) //Telemetry packet
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1]Telemetry packet received.");
                    PostPacket(PacketData.PacketType.Telemetry);
                }
            }
        }

        public void PostPacket(PacketData.PacketType type)
        {
            string typeStr = "NULL";
            string additionalMsg = "-";
            PacketData data = new UndefinedPacket(type);
            switch (type)
            {
                case PacketData.PacketType.Unknown:
                    typeStr = "Unknown";
                    break;
                case PacketData.PacketType.Telemetry:
                    typeStr = "Telemetry";
                    data = new TelemPacket(type);
                    break;
                case PacketData.PacketType.Image:
                    typeStr = "SSDV Image";
                    additionalMsg = "BG6LQV";
                    data = new ImagePacket(type, imageFilePath);
                    break;
            }

            PacketInfo packetInfo = new PacketInfo("ASRTU-1(AO-123)", typeStr, additionalMsg, data);
            SatDataUI.RegisterPacket(packetInfo);
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
            if (!Directory.Exists(resDirPath))
                Directory.CreateDirectory(resDirPath);
            if(!File.Exists(binaryFilePath))
                File.Create(binaryFilePath);
            if(!File.Exists(imageFilePath))
                File.Create(imageFilePath);

            // also, we need to clear the output image file to make sure the ssdv.exe can works properly
            using(FileStream stream = new FileStream(imageFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                stream.Flush();
                stream.Close();
                stream.Dispose();
            }
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

            Task.Run(() =>
            {
                process.WaitForExit();
                process.Close();

                if (SatDataUI.currentImagePacket != null && SatDataUI.currentImagePacket.imagePath == imageFilePath)
                {
                    SatDataUI.UpdateImage(); //Update UI
                }
            });

            return true;
        }
    }
}
