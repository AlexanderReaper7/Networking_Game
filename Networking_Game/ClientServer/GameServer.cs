using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Networking_Game.ClientServer
{
    /// <summary>
    /// The server for the client-server version of the game
    /// </summary>
    public class GameServer
    {
        private NetServer server;
        private List<NetPeer> clients;

        public void StartServer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId) { Port = Program.DefaultPort , EnableUPnP = true};
            server = new NetServer(config);
            server.Start();

            if (server.Status == NetPeerStatus.Running)
            {
                Console.WriteLine("Server is running on port " + config.Port);
                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine(server.UPnP.Status);
                    Console.WriteLine(server.UPnP.ForwardPort(Program.DefaultPort, "ree"));
                    Thread.Sleep(1500);
                }
            }
            else
            {
                Console.WriteLine("Server not started...");
                throw new Exception("Server not started");
            }
            clients = new List<NetPeer>();
        }

        public void ReadMessages()
        {
            NetIncomingMessage message;
            var stop = false;

            while (!stop)
            {
                while ((message = server.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            {
                                Console.WriteLine("I got smth!");
                                var data = message.ReadString();
                                Console.WriteLine(data);

                                if (data == "exit")
                                {
                                    stop = true;
                                }

                                break;
                            }
                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine(message.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            Console.WriteLine(message.SenderConnection.Status);
                            if (message.SenderConnection.Status == NetConnectionStatus.Connected)
                            {
                                clients.Add(message.SenderConnection.Peer);
                                Console.WriteLine("{0} has connected.", message.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            if (message.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                clients.Remove(message.SenderConnection.Peer);
                                Console.WriteLine("{0} has disconnected.", message.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            break;
                        default:
                            Console.WriteLine("Unhandled message type: {message.MessageType}");
                            break;
                    }
                    server.Recycle(message);
                }
            }

            Console.WriteLine("Shutdown package \"exit\" received. Press any key to finish shutdown");
            Console.ReadKey();
        }
    }
}
