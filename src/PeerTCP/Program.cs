using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Peer
{ 
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var myPort = args[0];
            var targetPort = args[1];
            var myName = args[2];

            var receiving = StartReceiving(myPort);
            var sending = StartSending(myName, targetPort);

            await Task.WhenAll(receiving, sending);
        }

        public async static Task StartSending(string name, string targetPort)
        {
            var client = new TcpClient();
            var localhost = IPAddress.Parse("::1");
            await client.ConnectAsync(localhost, int.Parse(targetPort));

            using (var networkStream = client.GetStream())
            {
                while (true)
                {
                    var message = Console.ReadLine();
                    var buffer = Encoding.UTF8.GetBytes($"{name}: {message}");
                    await networkStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        public async static Task StartReceiving(string myPort)
        {
            var listener = new TcpListener(IPAddress.Parse("::1"), int.Parse(myPort));
            listener.Start();
            Console.WriteLine($"Listening on localhost:{myPort}");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                using (var networkStream = client.GetStream())
                using (StreamReader reader = new StreamReader(networkStream, Encoding.UTF8))
                {
                    string message = await reader.ReadToEndAsync();
                    Console.WriteLine(message);

                }
                
                await Task.Delay(100);
            }
        }
    }
}
