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

        private void SendGameInformation()
        {
            
        }

        public void ReadMessages()
        {
            NetIncomingMessage message;
            bool run = true;

            while (run)
            {
                while ((message = server.ReadMessage()) != null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            ReadData(message);
                            break;
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
                        case NetIncomingMessageType.ConnectionApproval:
                            ReadConnectionAttempt(message);
                            break;
                        default:
                            Console.WriteLine($"Unhandled message type: {message.MessageType}");
                            break;
                    }
                    server.Recycle(message);
                }
            }
        }

        private void ReadConnectionAttempt(NetIncomingMessage message)
        {
            LoginCommand command = new LoginCommand(message);
            command.Run();
        }

        private void ReadData(NetIncomingMessage message)
        {
            // Read what type of packet was received
            PacketType packetType = (PacketType) message.ReadByte();

             message.Data
        }
    }
}
