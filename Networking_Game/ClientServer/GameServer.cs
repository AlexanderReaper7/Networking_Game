using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game.ClientServer
{
    public class PlayerConnection
    {
        public NetPeer peer;
        public Player player;

        public PlayerConnection(NetPeer peer, Player player)
        {
            this.peer = peer;
            this.player = player;
        }
    }

    /// <summary>
    /// The server for the client-server version of the game
    /// </summary>
    public class GameServer
    {
        private NetServer server;
        private List<PlayerConnection> clients;

        private GameType gameType;
        private Grid grid;

        public GameServer(GameType gameType, int gridSizeX, int gridSizeY, int port = Program.DefaultPort)
        {
            this.gameType = gameType;
            grid = new Grid(gridSizeX, gridSizeY);
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId) { Port = port, EnableUPnP = true };
            StartServer(config);
        }

        public void StartServer(NetPeerConfiguration config)
        {
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);

            server = new NetServer(config);
            server.Start();
            server.UPnP.ForwardPort(Program.DefaultPort, Program.AppId);

            if (server.Status == NetPeerStatus.Running)
            {
                Console.WriteLine("Server is running on port " + config.Port);
            }
            else
            {
                Console.WriteLine("Server not started...");
                throw new Exception("Server not started");
            }
            clients = new List<PlayerConnection>();
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
                                PlayerConnect(new PlayerConnection(inMsg.SenderConnection.Peer, (Player)ByteSerializer.ByteArrayToObject(inMsg.Data)));
                                Console.WriteLine("{0} has connected.", inMsg.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            if (inMsg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                PlayerDisconnect((from client in clients where client.peer == inMsg.SenderConnection.Peer select client).Single());
                                Console.WriteLine("{0} has disconnected.", inMsg.SenderConnection.Peer.Configuration.LocalAddress);
                            }
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            // Parse player data
                            Player newPlayer = (Player) ByteSerializer.ByteArrayToObject(inMsg.Data);
                            // Compare with existing players
                            foreach (PlayerConnection client in clients)
                            {
                                Player player = client.player;
                                // If name or shape and color already is used
                                if (string.Equals(newPlayer.Name, player.Name, StringComparison.CurrentCultureIgnoreCase)
                                    || (newPlayer.Shape == player.Shape && newPlayer.Color == player.Color))
                                {
                                    // If there is a match, reject connection
                                    inMsg.SenderConnection.Deny("Player already exists");
                                }
                            }
                            // If no match found, accept connection
                            inMsg.SenderConnection.Approve(); // TODO: send game data
                            break;

                        case NetIncomingMessageType.DiscoveryRequest:
                            server.SendDiscoveryResponse(server.CreateMessage($"{Program.AppId}"), inMsg.SenderEndPoint); // TODO: How to ident?
                            break;

                        default:
                            Console.WriteLine($"Unhandled message type: {inMsg.MessageType}");
                            break;
                    }
                    server.Recycle(inMsg);
                }
            }
        }

        private void ReadData(NetIncomingMessage message)
        {
            // Read what type of packet was received
            PacketType packetType = (PacketType) message.ReadUInt16();
            switch (packetType)
            {
                
            }
        }

        private void PlayerDisconnect(PlayerConnection client)
        {
            // Remove client from clients list
            clients.Remove(client);
            // Send PlayerDisconnect message to other players
            NetOutgoingMessage outMsg = client.CreatePlayerDisconnectedMessage();
            server.SendToAll(outMsg, client.peer.Connections.First(), NetDeliveryMethod.ReliableOrdered, 1);
        }

        private void PlayerConnect(PlayerConnection client)
        {
            // NOTE: At this point we assume the player have already been checked for duplications
            // Add client to clients list
            clients.Add(client);
            // Send PlayerConnected message to other players
            NetOutgoingMessage outMsg = client.CreatePlayerConnectedMessage();
            server.SendToAll(outMsg, client.peer.Connections.First(), NetDeliveryMethod.ReliableOrdered, 1);

        }
    }
}
