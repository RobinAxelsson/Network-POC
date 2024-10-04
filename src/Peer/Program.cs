﻿using System.Net;

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
            var client = new HttpClient();

            while (true)
            {
                var message = Console.ReadLine();
                var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{targetPort}");
                request.Headers.Connection.Add("keep-alive");
                request.Content = new StringContent($"{name}: {message}");
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
        }

        public async static Task StartReceiving(string myPort)
        {
            var listener = new HttpListener();
            listener.TimeoutManager.DrainEntityBody = TimeSpan.FromSeconds(30);
            listener.Prefixes.Add($"http://localhost:{myPort}/");
            Console.WriteLine($"Receiving on {myPort}");
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

                //var response = context.Response;
                //response.StatusCode = 201;

                //string responseMessage = "Request received successfully";
                //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseMessage);

                //response.ContentLength64 = buffer.Length;
                //await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                //response.OutputStream.Close();

                Thread.Sleep(100);
            }
        }
    }
}
