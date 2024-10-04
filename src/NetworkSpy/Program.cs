using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using System.Text;

namespace NetworkSpy
{
    internal class Program
    {
        public static void Main()
        {
            var devices = LibPcapLiveDeviceList.Instance;

            if (devices.Count < 1)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            using var device = devices[7]; //NPF_Loopback Adapter for loopback traffic capture - localhost...
            Console.WriteLine("Device: " + device.Name);
            Console.WriteLine("Description: " + device.Description);

             device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            int readTimeoutMilliseconds = 1000;

            Console.WriteLine("Capture started...");
            
            device.Open(mode: DeviceModes.Promiscuous, read_timeout: readTimeoutMilliseconds);

            device.StartCapture();
            Console.ReadLine();

            Console.WriteLine("Capture stopped...");

            Console.WriteLine(device.Statistics.ToString());
        }

        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();

            if(rawPacket.LinkLayerType == LinkLayers.Null)
            {
                var packet = (NullPacket)Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var tcpPacket = packet.Extract<TcpPacket>();
                if (tcpPacket != null && tcpPacket.HasPayloadData && tcpPacket.PayloadData.Length > 0)
                {
                    var message = GetHttpChatMessage(tcpPacket.PayloadData);

                    if (message == null) return;

                    Console.WriteLine($"{tcpPacket.SourcePort}->{tcpPacket.DestinationPort}: {message}");
                }
            }
        }

        private static string? GetHttpChatMessage(byte[] payloadData)
        {
            var payLoadData = Encoding.UTF8.GetString(payloadData);
            var headersBody = payLoadData.Split("\r\n\r\n");
            var headers = headersBody[0].Split("\r\n");

            if (!headers[0].Contains("HTTP"))
            {
                return null;
            }

            if (headersBody.Length > 1 && !string.IsNullOrEmpty(headersBody[1]))
            {
                return headersBody[1].Trim();
            }

            return null;
        }
    }
}
