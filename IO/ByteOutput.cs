using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.IO
{
    public class ByteOutput
    {
        public string filePath = "file.bin";
        public FileStream fileStream;
        private bool isStreamOpened = false;

        public ByteOutput(string filePath)
        {
            this.filePath = filePath;
        }

        public void OpenStream()
        {
            if(isStreamOpened)
                return;

            fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            isStreamOpened = true;
        }

        public void WriteBytes(byte[] bytes)
        {
            if (!isStreamOpened)
                OpenStream();

            fileStream.Write(bytes, 0, bytes.Length);
        }

        public void ClearFile()
        {
            if (!isStreamOpened)
                return;

            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.SetLength(0);
        }

        public void CloseStream()
        {
            if (!isStreamOpened)
                return;

            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
            isStreamOpened = false;
        }
    }
}
