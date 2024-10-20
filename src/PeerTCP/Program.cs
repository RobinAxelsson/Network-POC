﻿using System.Net.Sockets;
using System.Net;

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

            await Task.WhenAll([receiving, sending]);
        }

        public async static Task StartSending(string name, string targetPort)
        {

            var client = new TcpClient();

            while (true)
            {
                var message = Console.ReadLine();
                var buffer = Encoding.UTF8.GetBytes($"{name}: message");
                
                Console.WriteLine(content);
            }
        }

        public async static Task StartReceiving(string myPort)
        {
            var listener = new HttpListener();
            listener.TimeoutManager.DrainEntityBody = TimeSpan.FromSeconds(30);
            listener.Prefixes.Add($"http://localhost:{myPort}/");
            Console.WriteLine($"Listening on localhost:{myPort}");
            listener.Start();
            
            while (true)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;

                using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string requestBody = await reader.ReadToEndAsync();
                    Console.WriteLine(requestBody);
                }

                Thread.Sleep(100);
            }
        }
    }
}
