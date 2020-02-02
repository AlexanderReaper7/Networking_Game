using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game.ClientServer
{
    public class PlayerConnection
    {
        public NetConnection connection;
        public Player player;

        public PlayerConnection(NetConnection connection, Player player)
        {
            this.connection = connection;
            this.player = player;
        }
    }
    // TODO: recycle sent messages
    /// <summary>
    /// The server for the client-server version of the game
    /// </summary>
    public class GameServer
    {
        private NetServer server;
        private GameServerArguments arguments;
        private List<PlayerConnection> clients;

        private int playerTurnIndex;
        private Grid grid;

        public bool Running { get; private set; }

        /// <summary>
        /// Create a new GameServer from arguments
        /// </summary>
        public GameServer(GameServerArguments args)
        {
            grid = new Grid(args.sizeX, args.sizeY, args.maxPlayers, args.minPlayers);
            arguments = args;

            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.Port = args.port;
            config.AutoFlushSendQueue = true;
            StartServer(config);
        }

        /// <summary>
        /// Create a new GameServer from console input
        /// </summary>
        /// <param name="port">Server port</param>
        public GameServer(int port = Program.DefaultPort)
        {
            grid = Grid.GetGameSettingsInput();
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.Port = port;
            config.AutoFlushSendQueue = true;
            StartServer(config);
        }

        private void StartServer(NetPeerConfiguration config)
        {
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            //config.EnableUPnP = true;
            server = new NetServer(config);
            server.Start();
            //server.UPnP.ForwardPort(config.Port, Program.AppId); // TODO: Add UPnP

            if (server.Status == NetPeerStatus.Running)
            {
                Console.WriteLine($"Server is running on port {config.Port}");
            }
            else
            {
                Console.WriteLine("Server not started...");
                throw new Exception("Server not started");
            }
            clients = new List<PlayerConnection>();

            Run();
        }

        public void StopServer()
        {
            Running = false;
            server.Shutdown("Bye message"); // TODO: change bye message for shutdown?
        }

        public void Run()
        {
            Running = true;
            Console.WriteLine("Starting network loop");
            while (Running)
            {
                server.MessageReceivedEvent.WaitOne();
                NetIncomingMessage inMsg;
                while ((inMsg = server.ReadMessage()) != null)
                {
                    Console.WriteLine($"msg {inMsg.MessageType}");
                    switch (inMsg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            Process(inMsg);
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Console.WriteLine(inMsg.ReadString());
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            Console.WriteLine(inMsg.SenderConnection.Status);
                            switch (inMsg.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Connected:
                                    PlayerConnection c = GetPlayerConnection(inMsg.SenderConnection);
                                    Console.WriteLine($"{c.player.Name} has connected from {c.connection.RemoteEndPoint}.");
                                    SendGameData(c);
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    // TODO: mimic connected
                                    PlayerDisconnect((from client in clients where client.connection == inMsg.SenderConnection select client).Single());
                                    Console.WriteLine($"{inMsg.SenderConnection.Peer.Configuration.LocalAddress} has disconnected.");
                                    break;
                            }

                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            // Compare existing connections
                            if ((from client in clients where client.connection == inMsg.SenderConnection select client)
                                .Count() != 0)
                            {
                                // If there is an existing connection, reject connection
                                inMsg.SenderConnection.Deny("Connection already exists");
                                break;
                            }

                            // Parse player data
                            Player newPlayer = (Player) ByteSerializer.ByteArrayToObject(inMsg.Data);

                            // Compare with existing players
                            // If name or shape and color already is used
                            if ((from client in clients
                                    where string.Equals(newPlayer.Name, client.player.Name, StringComparison.CurrentCultureIgnoreCase)
                                          || (newPlayer.Shape == client.player.Shape && newPlayer.Color == client.player.Color)
                                    select client)
                                .Count() != 0)
                            {
                                // If there is a match, reject connection
                                inMsg.SenderConnection.Deny("Player already exists");
                                break;
                            }

                            // If no match found, accept connection
                            PlayerConnect(new PlayerConnection(inMsg.SenderConnection, (Player)ByteSerializer.ByteArrayToObject(inMsg.Data)));
                            inMsg.SenderConnection.Approve(); 
                            // Send game data
                            break;

                        case NetIncomingMessageType.DiscoveryRequest:
                            Console.WriteLine("Discovery Request from Client");
                            server.SendDiscoveryResponse(server.CreateMessage($"{Program.AppId}"), inMsg.SenderEndPoint); // TODO: How to ident?
                            break;

                        default:
                            Console.WriteLine($"Unhandled message type: {inMsg.MessageType}");
                            break;
                    }
                    server.Recycle(inMsg);
                }
            }

            Running = false;
        }

        private void Process(NetIncomingMessage inMsg)
        {
            // TODO: stop server on game end
            // Read what type of packet was received
            PacketType packetType = (PacketType)inMsg.ReadUInt16();
            switch (packetType)
            {
                case PacketType.ClaimSquare:
                    ClaimSquare(inMsg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClaimSquare(NetIncomingMessage inMsg)
        {
                    var player = GetPlayerConnection(inMsg.SenderConnection).player;
                    // Make sure it is this players turn 
                    if (player != clients[playerTurnIndex % clients.Count].player)
                    {
                        // Send failed message back
                        //var outMsg = PacketFactory.CreateFailedClaimSquareMessage(server, "Not your turn");
                        //server.SendMessage(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        //return;
                    }

                    int x, y;
                    try
                    {
                        // Parse position
                        x = inMsg.ReadInt32();
                        y = inMsg.ReadInt32();
                    }
                    catch (Exception)
                    {
                        // Send failed message back
                        var outMsg = PacketFactory.CreateFailedClaimSquareMessage(server, "Failed to parse position");
                        server.SendMessage(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        return;
                    }

                    // Claim square
                    // If successfully claimed square,
                    if (grid.ClaimSquare(new Point(x, y), player))
                    {

                        // Send claim square message to other players
                        var outMsg = player.CreateClaimSquareMessage(server, x, y);
                        server.SendToAll(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered,0);

                        // If there are still squares to fill
                        if (playerTurnIndex < grid.Squares.Length -1)
                        {
                            // Start next turn
                            NextTurn();
                        }
                        else
                        {
                            // End game
                            EndGame();
                        }
                    }
                    else
                    {
                        // Send failed message back
                        //var outMsg = PacketFactory.CreateFailedClaimSquareMessage(server, "Could not claim square");
                        //server.SendMessage(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        //return;
                    }

        }

        private void SendGameData(PlayerConnection recipient)
        {
            var outMsg = server.CreateMessage();

            // Grid
            outMsg = grid.CreateGridDataMessage(server);
            server.SendMessage(outMsg, recipient.connection, NetDeliveryMethod.ReliableOrdered);
            // Players
            foreach (PlayerConnection c in clients)
            {
                if (c == recipient) continue; // Skip recipient
                outMsg = c.CreatePlayerConnectedMessage(server);
                server.SendMessage(outMsg, recipient.connection, NetDeliveryMethod.ReliableOrdered);
            }
            // Turn
            outMsg = clients[playerTurnIndex % clients.Count].CreateNextTurnMessage(server);
            server.SendMessage(outMsg, recipient.connection, NetDeliveryMethod.ReliableOrdered);
        }

        private void PlayerDisconnect(PlayerConnection client)
        {
            // Remove client from clients list
            clients.Remove(client);
            // Send PlayerDisconnect message to other players
            NetOutgoingMessage outMsg = client.CreatePlayerDisconnectedMessage(server);
            server.SendToAll(outMsg, client.connection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void PlayerConnect(PlayerConnection client)
        {
            // NOTE: At this point we assume the player have already been checked for duplications
            // Add client to clients list
            clients.Add(client);
            // if there are others than the new client,
            if (clients.Count > 1)
            {
                // Send PlayerConnected message to other players
                NetOutgoingMessage outMsg = client.CreatePlayerConnectedMessage(server);
                server.SendToAll(outMsg, client.connection, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        private void NextTurn()
        {
            playerTurnIndex++;
            var client = clients[playerTurnIndex % clients.Count];
            server.SendToAll(client.CreateNextTurnMessage(server),  NetDeliveryMethod.ReliableOrdered);
        }

        private void EndGame()
        {
            // Send end game message
            var outMsg = PacketFactory.CreateEndGameMessage(server);
            server.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
            server.FlushSendQueue();
            Thread.Sleep(3000);
            // End server
            StopServer();
        }

        private PlayerConnection GetPlayerConnection(NetConnection connection)
        {
            return (from client in clients where connection == client.connection select client).Single();
        }
    }

    [Serializable]
    public struct GameServerArguments
    {
        public int sizeX;
        public int sizeY;

        public int maxPlayers;
        public int minPlayers;

        public int port;

        public static GameServerArguments CreateFromConsoleInput()
        {
            GameServerArguments args = new GameServerArguments();
            string s;
            // Get sizeX
            do s = ConsoleManager.WaitGetPriorityInput("Input grid size along the X-axis: ", false);
            while (int.TryParse(s, out args.sizeX));
            // Get sizeY
            do s = ConsoleManager.WaitGetPriorityInput("Input grid size along the Y-axis: ", false);
            while (int.TryParse(s, out args.sizeY));
            // Get maxPlayers
            do s = ConsoleManager.WaitGetPriorityInput("Input maximum players: ", false);
            while (int.TryParse(s, out args.maxPlayers));
            // Get minPlayers
            do s = ConsoleManager.WaitGetPriorityInput("Input minimum players: ", false);
            while (int.TryParse(s, out args.minPlayers));
            // Get port
            do s = ConsoleManager.WaitGetPriorityInput("Input server port: ", false);
            while (int.TryParse(s, out args.port));

            return args;
        }

        // TODO: Use reflection instead?
        public override string ToString()
        {
            return $"{sizeX}|{sizeY}|{maxPlayers}|{minPlayers}|{port}";
        }

        public bool TryParse(string s, out GameServerArguments args)
        {
            try
            {
                args = Parse(s);
                return true;
            }
            catch (Exception)
            {
                args = default;
                return false;
            }
        }

        public GameServerArguments Parse(string s)
        {
            // Separate string
            string[] ss = s.Split('|');

            int[] ints = new int[5];

            for (int i = 0; i < 5; i++)
            {
                ints[i] = int.Parse(ss[i]);
            }

            GameServerArguments output = new GameServerArguments();

            output.sizeX = ints[0];
            output.sizeY = ints[1];
            output.maxPlayers = ints[2];
            output.minPlayers = ints[3];
            output.port = ints[4];

            return output;
        }
    }
}
