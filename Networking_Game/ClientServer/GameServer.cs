using System;
using System.Collections.Generic;
using System.Linq;
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
    ///     The server for the client-server version of the game
    /// </summary>
    public class GameServer
    {
        GameServerArgument argument;
        List<PlayerConnection> clients;
        readonly Grid grid;
        int playerTurnIndex;
        NetServer server;

        /// <summary>
        ///     Create a new GameServer from argument
        /// </summary>
        public GameServer(GameServerArgument args)
        {
            grid = new Grid(args.sizeX, args.sizeY, args.maxPlayers, args.minPlayers);
            argument = args;

            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.Port = args.port;
            config.AutoFlushSendQueue = true;
            StartServer(config);
        }

        /// <summary>
        ///     Create a new GameServer from console input
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

        public bool Running { get; private set; }

        public void StopServer()
        {
            Running = false;
            server.Shutdown("Bye message"); // Q: change bye message for shutdown?
        }

        public void Run()
        {
            Running = true;
            Console.WriteLine("Starting network loop");
            while (Running)
            {
                server.MessageReceivedEvent.WaitOne(); // Q: should we wait every loop or only the first?
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
                                    // Check if there are enough players to start the game
                                    if (clients.Count >= grid.minPlayers)
                                    {
                                        // TODO: start voting to start game
                                        // Start game
                                        server.SendToAll(PacketFactory.CreateStartGameMessage(server), NetDeliveryMethod.ReliableOrdered);
                                    }

                                    break;

                                case NetConnectionStatus.Disconnected:
                                    PlayerConnection pc = GetPlayerConnection(inMsg.SenderConnection);
                                    Console.WriteLine($"{pc.player.Name} has disconnected.");
                                    PlayerDisconnect(pc);
                                    break;
                            }

                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            // Compare existing connections
                            if ((from client in clients where client.connection == inMsg.SenderConnection select client).Count() != 0)
                            {
                                // If there is an existing connection, reject connection
                                string reason = "Connection already exists";
                                Console.WriteLine($"Player failed to connect: {reason}");
                                inMsg.SenderConnection.Deny(reason);
                                break;
                            }

                            // Check max players
                            if (clients.Count < grid.maxPlayers == false)
                            {
                                string reason = "Max players reached";
                                Console.WriteLine($"Player failed to connect: {reason}");
                                inMsg.SenderConnection.Deny(reason);
                                break;
                            }

                            // Parse player data
                            Player newPlayer = (Player) ByteSerializer.ByteArrayToObject(inMsg.Data);

                            // Compare with existing players
                            // If name or shape and drawingColor already is used
                            if ((from client in clients
                                    where string.Equals(newPlayer.Name, client.player.Name, StringComparison.CurrentCultureIgnoreCase) //TODO: refactor into two where
                                          || newPlayer.Shape == client.player.Shape && newPlayer.Color == client.player.Color
                                    select client).Count() != 0)
                            {
                                // If there is a match, reject connection
                                string reason = "Player already exists";
                                Console.WriteLine($"Player failed to connect: {reason}");
                                inMsg.SenderConnection.Deny(reason);
                                break;
                            }

                            // If no match found, accept connection
                            PlayerConnect(new PlayerConnection(inMsg.SenderConnection, (Player) ByteSerializer.ByteArrayToObject(inMsg.Data)));
                            inMsg.SenderConnection.Approve();
                            // Send game data
                            break;

                        case NetIncomingMessageType.DiscoveryRequest: // TODO: send found server data
                            Console.WriteLine("Discovery Request from Client");
                            NetOutgoingMessage outMsg = server.CreateMessage();
                            FoundServer v = new FoundServer(grid.sizeX, grid.sizeY, grid.maxPlayers, grid.minPlayers, clients.Count);
                            outMsg.Write(ByteSerializer.ObjectToByteArray(v));
                            server.SendDiscoveryResponse(outMsg, inMsg.SenderEndPoint);
                            break;

                        default:
                            Console.WriteLine($"Unhandled message received of type: {inMsg.MessageType}");
                            break;
                    }

                    server.Recycle(inMsg);
                }
            }

            Running = false;
        }

        void StartServer(NetPeerConfiguration config)
        {
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            //config.EnableUPnP = true;
            server = new NetServer(config);
            server.Start();
            //server.UPnP.ForwardPort(config.Port, Program.AppId); // TODO: Add UPnP

            if (server.Status == NetPeerStatus.Running) Console.WriteLine($"Server is running on port {config.Port}");
            else
            {
                Console.WriteLine("Server not started...");
                throw new Exception("Server not started");
            }

            clients = new List<PlayerConnection>();

            Run();
        }

        void Process(NetIncomingMessage inMsg)
        {
            // TODO: stop server on game end
            // Read what type of packet was received
            PacketType packetType = (PacketType) inMsg.ReadUInt16();
            switch (packetType)
            {
                case PacketType.ClaimSquare:
                    ClaimSquare(inMsg);
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }

        void ClaimSquare(NetIncomingMessage inMsg)
        {
            Player player = GetPlayerConnection(inMsg.SenderConnection).player;
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
                NetOutgoingMessage outMsg = PacketFactory.CreateFailedClaimSquareMessage(server, "Failed to parse position");
                server.SendMessage(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                return;
            }

            // Claim square
            // If successfully claimed square,
            if (grid.ClaimSquare(new Point(x, y), player))
            {
                // Send claim square message to other players
                NetOutgoingMessage outMsg = player.CreateClaimSquareMessage(server, x, y);
                server.SendToAll(outMsg, inMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

                // If there are still squares to fill
                if (playerTurnIndex < grid.Squares.Length - 1)
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
        }

        void SendGameData(PlayerConnection recipient)
        {
            NetOutgoingMessage outMsg = server.CreateMessage();

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

        void PlayerDisconnect(PlayerConnection client)
        {
            // Remove client from clients list
            clients.Remove(client);
            // Send PlayerDisconnect message to other players
            NetOutgoingMessage outMsg = client.CreatePlayerDisconnectedMessage(server);
            server.SendToAll(outMsg, client.connection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        void PlayerConnect(PlayerConnection client)
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

        void NextTurn()
        {
            playerTurnIndex++;
            PlayerConnection client = clients[playerTurnIndex % clients.Count];
            server.SendToAll(client.CreateNextTurnMessage(server), NetDeliveryMethod.ReliableOrdered);
        }

        void EndGame()
        {
            // Send end game message
            NetOutgoingMessage outMsg = PacketFactory.CreateEndGameMessage(server);
            server.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
            server.FlushSendQueue();
            Thread.Sleep(3000);
            // End server
            StopServer();
        }

        PlayerConnection GetPlayerConnection(NetConnection connection) { return (from client in clients where connection == client.connection select client).Single(); }
    }

    [Serializable]
    public struct GameServerArgument
    {
        public int sizeX;
        public int sizeY;

        public int maxPlayers;
        public int minPlayers;

        public int port;

        public static GameServerArgument CreateFromConsoleInput()
        {
            GameServerArgument args = new GameServerArgument();
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

        // Q: Use reflection instead?
        public override string ToString() { return $"{sizeX}|{sizeY}|{maxPlayers}|{minPlayers}|{port}"; }

        public bool TryParse(string s, out GameServerArgument args)
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

        public GameServerArgument Parse(string s)
        {
            // Separate string
            string[] ss = s.Split('|');

            int[] ints = new int[5];

            for (int i = 0; i < 5; i++) ints[i] = int.Parse(ss[i]);

            GameServerArgument output = new GameServerArgument();

            output.sizeX = ints[0];
            output.sizeY = ints[1];
            output.maxPlayers = ints[2];
            output.minPlayers = ints[3];
            output.port = ints[4];

            return output;
        }
    }
}