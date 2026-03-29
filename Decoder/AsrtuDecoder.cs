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
            string ssdvArgs = GenerateSSDVDecoderArgs();
            startInfo = new ProcessStartInfo(ssdvDecoderPath, $"-d {binaryFilePath} {imageFilePath}");
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;

            CheckOutputFiles();
        }

        public void ProcessPacket(byte[] packet)
        {
            packet.FastCopyTo(dataBuffer, packet.Length);
            if (packet[0] == 0x03)
            {
                if (packet[1] == 0x22)
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1]SSDV packet received.");
                    ReplaceSSDVHeader();
                    byteOutput.WriteBytes(dataBuffer);
                    DecodeSSDV();
                }
                else if (packet[1] == 0x24)
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1]Telemetry packet received.");
                }
            }
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

        private string GenerateSSDVDecoderArgs(params string[] args)
        {
            string s = "";
            foreach (string arg in args)
            {
                s = s + arg + " ";
            }
            s = s.Trim();
            return s;
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
