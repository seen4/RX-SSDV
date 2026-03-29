using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Decoder
{
    public interface ITransportDecoder
    {
        public void ProcessPacket(byte[] packet);
    }
}
