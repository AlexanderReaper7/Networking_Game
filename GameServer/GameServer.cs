using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;

namespace Networking_Game.ClientServer
{
    /// <summary>
    /// The server for the client-server version of the game
    /// </summary>
    public class GameServer : GameCore
    {
        private NetServer server;
        private List<NetPeer> clients;

        public void StartServer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId) { Port = Program.DefaultPort , EnableUPnP = true};
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);

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
            NetIncomingMessage inMsg;
            bool run = true;

            while (run)
            {
                while ((inMsg = server.ReadMessage()) != null)
                {
                    switch (inMsg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            ReadData(inMsg);
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine(inMsg.ReadString());
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            Console.WriteLine(inMsg.SenderConnection.Status);
                            if (inMsg.SenderConnection.Status == NetConnectionStatus.Connected)
                            {
                                clients.Add(inMsg.SenderConnection.Peer);
                                Console.WriteLine("{0} has connected.", inMsg.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            if (inMsg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                clients.Remove(inMsg.SenderConnection.Peer);
                                Console.WriteLine("{0} has disconnected.", inMsg.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            // Parse player data
                            Player newPlayer = (Player) ByteSerializer.ByteArrayToObject(inMsg.Data);
                            // Compare with existing players
                            foreach (Player player in players)
                            {
                                if (string.Equals(newPlayer.Name, player.Name, StringComparison.CurrentCultureIgnoreCase)
                                    || (newPlayer.Shape == player.Shape && newPlayer.Color == player.Color))
                                {
                                    // If there is a match, reject connection
                                    inMsg.SenderConnection.Deny("Player already exists");
                                }
                            }
                            // If no match found, accept connection
                            inMsg.SenderConnection.Approve(); // TODO: send game data
                            // Send player join data to other players
                            NetOutgoingMessage outMsg = server.CreateMessage();
                            outMsg.Data = inMsg.Data;
                            server.SendToAll(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);
                            break;

                        case NetIncomingMessageType.DiscoveryRequest:
                            
                            break;

                        default:
                            Console.WriteLine($"Unhandled message type: {inMsg.MessageType}");
                            break;
                    }
                    server.Recycle(inMsg);
                }
            }
        }

        private void ReadConnectionAttempt(NetIncomingMessage message)
        {
            LoginCommand command = new LoginCommand();
            command.Run(this, message);
        }

        private void ReadData(NetIncomingMessage message)
        {
            // Read what type of packet was received
            PacketType packetType = (PacketType) message.ReadByte();

             
        }
    }
}
