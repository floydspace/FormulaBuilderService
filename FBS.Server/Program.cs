﻿using System;
using System.Net;
using System.Text;
using FBS.Builder;

namespace FBS.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new AppConfig();
            var port = config.Port ?? 2200;

            var tcpServer = new TcpServer(IPAddress.Any, port);
            tcpServer.ClientConnected += tcpServer_ClientConnected;
            tcpServer.ClientDisconnected += client =>
            {
                Console.WriteLine("Client disconected: {0}", client.RemoteEndPoint);
            };
            tcpServer.Start(server => Console.WriteLine("SERVER STARTED ON PORT: {0}!", port));

            var exitOrder = false;
            do
            {
                var input = Console.ReadLine() ?? "";
                if (input.Equals("exit"))
                {
                    tcpServer.Stop(server => Console.WriteLine("SERVER TERMINATED!"));
                    exitOrder = true;
                }
                if (input.Equals("clients"))
                {
                    tcpServer.ConnectedClients.ForEach(client => Console.WriteLine(client.RemoteEndPoint));
                }
                if (input.StartsWith("disconnect"))
                {
                    tcpServer.ConnectedClients[int.Parse(input.Split(' ')[1])].Disconnect();
                }
            } while (!exitOrder);
        }

        private static void tcpServer_ClientConnected(TcpClient client)
        {
            Console.WriteLine("Client connected: {0}", client.RemoteEndPoint);
            client.Received += (c, r) => Console.WriteLine("← Request received: {0} from client {1}", r, client.RemoteEndPoint);
            client.Sent += (c, r) => Console.WriteLine("→ Response sent: {0} to client {1}", r, client.RemoteEndPoint);

            while (client.IsConnected)
            {
                var receivedString = client.ReceiveString(Encoding.UTF8);
                if (receivedString.Equals("DisconnectEvent") || string.IsNullOrEmpty(receivedString))
                {
                    client.Disconnect();
                    break;
                }

                var builder = new FormulaBuilder(receivedString);
                var sendingString = builder.Build();

                client.SendString(sendingString, Encoding.UTF8);
            }
        }
    }
}
