using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Decoder
{
    public class PacketInfo
    {
        public string satelliteName = "SAT-NAME";
        public string packetTypeStr = "PACKET-TYPE";
        public string msg = "";

        public string SatelliteName => satelliteName;
        public string PacketTypeStr => packetTypeStr;
        public string Msg => msg;

        public PacketData data;

        public PacketInfo(string satelliteName, string packetTypeStr, string msg, PacketData data)
        {
            this.satelliteName = satelliteName;
            this.packetTypeStr = packetTypeStr;
            this.msg = msg;
            this.data = data;
        }
    }

    public abstract class PacketData
    {
        public enum PacketType
        {
            Unknown = 0,
            Telemetry = 1,
            Image = 2
        }

        public PacketType packetType = PacketType.Unknown;

        public PacketData(PacketType packetType)
        {
            this.packetType = packetType;
        }
    }

    public class UndefinedPacket : PacketData
    {
        //Just nothing

        public UndefinedPacket(PacketType packetType) : base(packetType)
        {

        }
    }

    public class ImagePacket : PacketData
    {
        public string imagePath = "";

        public ImagePacket(PacketType packetType, string imagePath) : base(packetType)
        {
            this.imagePath = imagePath;
        }
    }

    public class TelemPacket : PacketData
    {
        //TODO

        public TelemPacket(PacketType packetType) : base(packetType)
        {

        }
    }
}
