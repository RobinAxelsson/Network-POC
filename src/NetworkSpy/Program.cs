using System.Text.Json;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using System.Net.Sockets;
using PacketDotNet.Tcp;
using System.Diagnostics.Tracing;
using System.Text;

namespace NetworkSpy
{
    internal class Program
    {
        private static CaptureFileWriterDevice captureFileWriter;
        public static void Main()
        {
            var devices = LibPcapLiveDeviceList.Instance;

            if (devices.Count < 1)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            //using var device = devices[0]; //Hyper-V
            //using var device = devices[1]; //Microsoft Wi-Fi Direct Virtual Adapter
            //using var device = devices[2]; //Intel(R) Dual Band Wireless-AC 7260ls
            //using var device = devices[3]; //Bluetooth Device (Personal Area Network)
            //using var device = devices[4]; //Generic Mobile Broadband Adapter
            //using var device = devices[5]; //Microsoft Wi-Fi Direct Virtual Adapter
            //using var device = devices[6]; //Intel(R) Ethernet Connection I217-LM -- Internet kabeln funkar
            using var device = devices[7]; //NPF_Loopback Adapter for loopback traffic capture - localhost...


            Console.WriteLine(device.Name);
            Console.WriteLine(device.Description);

             device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            int readTimeoutMilliseconds = 1000;
            
            device.Open(mode: DeviceModes.Promiscuous, read_timeout: readTimeoutMilliseconds);

            var capFile = Path.GetTempPath() + "\\capture.txt";
            
            Console.WriteLine();
            Console.WriteLine("-- Listening on {0} {1}, writing to {2}, hit 'Enter' to stop...",
                              device.Name, device.Description,
                              capFile);

            captureFileWriter = new CaptureFileWriterDevice(capFile);
            captureFileWriter.Open(device);

            device.StartCapture();

            Console.ReadLine();

            device.StopCapture();
            
            Console.WriteLine("-- Capture stopped.");

            // Print out the device statistics
            Console.WriteLine(device.Statistics.ToString());
        }

        private static int PacketIndex = 0;
        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            Console.WriteLine("Packet: " + ++PacketIndex);
            var packet = (NullPacket)Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var packetString = packet.ToString(StringOutputType.Normal).Replace(" ", "\n");
            packetString = packetString.Replace("8081", "8081 (Carl)");
            Console.WriteLine(packetString);

            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket.HasPayloadData)
            {
                Console.WriteLine("PayloadData:" + Encoding.UTF8.GetString(tcpPacket.PayloadData));
            }

            Console.WriteLine("-----------------------");
            Console.WriteLine();
            //Console.WriteLine("Packet:");
            //var iPv6Packet = packet.Extract<IPv6Packet>();
            //if (iPv6Packet != null)
            //{

            //}

            //var tcpPacket = packet.Extract<TcpPacket>();
            //if (tcpPacket != null)
            //{

            //}
            //Console.WriteLine(new PacketWrapper(packet));
        }

        private static string ToHexString(byte[]? data){
            if(data != null)
                return BitConverter.ToString(data).Replace("-", " ");
            return "";
        }

        //Layer 2 Data Link Layer: resposible for getting data from one network device to another on the same network 
        //14 bytes (allways)

        private class PacketWrapper
        {
            public string RawPayLoad { get; }
            public int TotalPacketLength { get; }
            public string NullHeader { get; }
            public IpHeader IpHeader { get; }
            public TcpHeader TcpHeader { get; }
            //public string PayLoad { get; } //Layer 5-7 Application Layer

            public int PacketLength { get; set; }
            public PacketWrapper(NullPacket packet)
            {
                RawPayLoad = ToHexString(packet.Bytes);
                TotalPacketLength = packet.TotalPacketLength;
                NullHeader = ToHexString(packet.HeaderData);
                var iPv6Packet = packet.Extract<IPv6Packet>();
                if (iPv6Packet != null)
                {
                    IpHeader = new IpHeader(iPv6Packet);
                }

                var tcpPacket = packet.Extract<TcpPacket>();
                if (tcpPacket != null) {
                    TcpHeader = new TcpHeader(tcpPacket);
                }
                //PayLoad = ToHexString(data[52..]);
            }

            public override string ToString()
            {
                return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        //Layer 3 Network Layer: handles routing and logical addressing (IP:s)
        //20-60 bytes varies because of the option field
        //IHL internet header length field - how many 32-bit words (4 byte units) AA-1F-C2-FF 
        private class IpHeader
        {
            public string RawPayload { get; }
            public string Version { get; }
            public int TrafficClass { get; }
            public string DestinationAddress { get; }
            public string SourceAddress { get; }
            public int FlowLabel { get; }
            public string NextHeader { get; }
            public int HopLimit { get; }
            public int PayLoadLength { get; }
            public IpHeader(IPv6Packet ipV6Packet)
            {
                RawPayload = ToHexString(ipV6Packet.HeaderData);
                Version = ipV6Packet.Version.ToString();
                TrafficClass = ipV6Packet.TrafficClass;
                DestinationAddress = ipV6Packet.DestinationAddress.ToString();
                SourceAddress = ipV6Packet.SourceAddress.ToString();
                FlowLabel = ipV6Packet.FlowLabel;
                NextHeader = ipV6Packet.NextHeader.ToString();
                HopLimit = ipV6Packet.HopLimit;
                PayLoadLength = ipV6Packet.PayloadLength;
            }
        }

        private class TcpHeader
        {
            public string RawPayload { get; }
            public string SourcePort { get; }
            public string DestinationPort { get; }
            public uint SequenceNumber { get; }
            public int DataOffset { get; }
            public ushort Checksum { get; }
            public bool ValidTcpChecksum { get; }
            public ushort WindowSize { get; }
            public string Flags { get; }

            public TcpHeader(TcpPacket tcpPacket)
            {
                RawPayload = ToHexString(tcpPacket.HeaderData);
                SourcePort = tcpPacket.SourcePort.ToString();
                DestinationPort = tcpPacket.DestinationPort.ToString();
                SequenceNumber = tcpPacket.SequenceNumber;
                DataOffset = tcpPacket.DataOffset;
                Checksum = tcpPacket.Checksum;
                ValidTcpChecksum = tcpPacket.ValidTcpChecksum;
                WindowSize = tcpPacket.WindowSize;
                var flags = ""; 
                
                if(tcpPacket.Acknowledgment) flags += "ACK ";
                if(tcpPacket.CongestionWindowReduced) flags += "CWR ";
                if(tcpPacket.ExplicitCongestionNotificationEcho) flags += "ECE ";
                if(tcpPacket.Urgent) flags += "URG" ; 
                if(tcpPacket.Push) flags += "PSH ";
                if (tcpPacket.Reset) flags += "RST" ; 
                if(tcpPacket.Synchronize) flags += "SYN ";
                if (tcpPacket.Finished) flags += "FIN" ;

                Flags = flags;
            }
        }
    }
}
