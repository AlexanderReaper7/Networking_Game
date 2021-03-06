﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game.ClientServer
{
    /// <summary>
    ///     The client for the client-server version of the game
    /// </summary>
    public class GameClient : GameCore
    {
        NetClient client;
        bool gameActive;
        Dictionary<IPEndPoint, FoundServer> gameServers;
        bool isMessageReadLoopRunning;
        Player localPlayer;
        Thread messageHandlerThread, gameActivityThread;

        protected override void Initialize()
        {
            // Create player
            localPlayer = Player.GetPlayerSettingsInput();
            players.Add(localPlayer);
            // Create Cached version of localPlayer as byte array 
            // byte[] TEMP = ByteSerializer.ObjectToByteArray(localPlayer); TODO: Cache byte array localPlayer

            StartClient();

            gameServers = new Dictionary<IPEndPoint, FoundServer>();
            Thread.Sleep(1000);
            // Discover servers
            DiscoverServers();
            gameActivityThread = new Thread(ActivityCheck) {Name = nameof(gameActivityThread)};
            gameActivityThread.Start();

            action += WaitForGameActive;
            DirectConnectServer(gameServers.Keys.ToArray()[0]);
            base.Initialize();
        }

        /// <summary>
        ///     Gets client port from console input
        /// </summary>
        /// <returns></returns>
        static int GetClientPort()
        {
            int port;
            string s;

            // Get client port
            do s = ConsoleManager.WaitGetPriorityInput("Input client port: ", false);
            while (!int.TryParse(s, out port));

            return port;
        }

        static IPEndPoint GetServerAddress()
        {
            IPAddress ip;
            int port;

            // Request server ip from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input server IP adress: ", false);
                // Parse input
                if (IPAddress.TryParse(input, out ip)) break;
                Console.WriteLine($"{input} is not a valid IP adress.");
            }

            // Request server port from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input server port: ", false);
                // parse input
                if (int.TryParse(input, out port)) break;
                Console.WriteLine($"{input} is not a valid port.");
            }

            return new IPEndPoint(ip, port);
        }

        void WaitForGameActive()
        {
            while (!gameActive) Thread.Sleep(333);

            action -= WaitForGameActive;
        }

        void confcam()
        {
            ConfigureCamera();
            action -= confcam;
        }

        void ActivityCheck()
        {
            // If no game is active
            if (!gameActive)
            {
                // Ask user for command
                string s = ConsoleManager.WaitGetPriorityInput("Input command: ", false);
                if (Enum.TryParse(s, true, out Commands command))
                {
                    switch (command)
                    {
                        case Commands.ListServers:
                            ListFoundServers();
                            break;

                        case Commands.Connect:
                            DirectConnectServer(GetServerAddress());
                            break;

                        case Commands.Discover:
                            DiscoverServers();
                            break;

                        case Commands.Help:
                        case Commands.Commands:
                            // List commands
                            Console.WriteLine("Commands:");
                            foreach (string c in Enum.GetNames(typeof(Commands))) Console.WriteLine(c);
                            break;

                        default:
                            Console.WriteLine("Unrecognized command");
                            break;
                    }
                }
                else Console.WriteLine("Unrecognized command");
            }
            else Thread.Sleep(1000);
        }

        void StartClient()
        {
            // TODO: add NAT/UPnP
            // Create config
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.AutoFlushSendQueue = true;
            // Get client port NOTE: By default set to 0 which the OS assigns to an available port
            //config.Port = GetClientPort(); 

            // Create client
            client = new NetClient(config);
            client.Start();

            // Start message loop
            StartMessageReaderThread();
        }

        void StartMessageReaderThread()
        {
            messageHandlerThread = new Thread(() =>
            {
                isMessageReadLoopRunning = true;
                while (isMessageReadLoopRunning) ReadMessages();
            });
            messageHandlerThread.Name = nameof(ReadMessages);
            messageHandlerThread.Start();
        }

        /// <summary>
        ///     Uses network discovery to find servers
        /// </summary>
        void DiscoverServers()
        {
            // Discover on default port
            Colorful.Console.WriteLine("Discovering Servers...");
            client.DiscoverLocalPeers(Program.DefaultPort);

            // Check for answers after x seconds
            Thread.Sleep(2000);
            Console.WriteLine($"Found {gameServers.Count} server(s)");

            // Ask user if to continue to discover on custom port
            do
            {
                // Get input
                string s = ConsoleManager.WaitGetPriorityInput("Input server port to continue discovering or leave empty to stop");
                if (s.Length > 0)
                {
                    // Parse input
                    if (int.TryParse(s, out int p))
                    {
                        Colorful.Console.WriteLine("Discovering Servers...");
                        client.DiscoverLocalPeers(p);
                        Thread.Sleep(1200);
                        Console.WriteLine($"Found {gameServers.Count} server(s)");
                    }
                    else Console.WriteLine("input is not a valid port");
                }
                else break; // If input is empty stop discovery
            }
            while (true);
        }


        /// <summary>
        ///     Attempts to connect to a server
        /// </summary>
        void DirectConnectServer(IPEndPoint endPoint)
        {
            // Create player data
            byte[] arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
            NetOutgoingMessage om = client.CreateMessage(arrPlayer.Length);
            om.Write(arrPlayer);
            // Connect to server
            client.Connect(endPoint.Address.ToString(), endPoint.Port, om);
        }

        void ReadMessages()
        {
            NetIncomingMessage inMsg;
            if ((inMsg = client.ReadMessage()) != null)
            {
#if DEBUG
                Console.WriteLine($"msg: {inMsg.MessageType}");
#endif
                switch (inMsg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // Handle custom messages
                        Process(inMsg);
                        break;

                    case NetIncomingMessageType.DiscoveryResponse:
                        // Check if server already is found
                        if (gameServers.ContainsKey(inMsg.SenderEndPoint)) break;
                        // Parse server status data
                        FoundServer fs = ByteSerializer.ByteArrayToObject<FoundServer>(inMsg.Data);
                        // Add server to list
                        gameServers.Add(inMsg.SenderEndPoint, fs);

                        Console.WriteLine($"Discovered server at: {inMsg.SenderEndPoint}");
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (inMsg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                            case NetConnectionStatus.Disconnecting:
                            case NetConnectionStatus.Disconnected:
                                Console.WriteLine($"{inMsg.SenderConnection.Status} from {inMsg.SenderConnection.RemoteEndPoint}");
                                break;
                        }

                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages (only received when compiled in DEBUG mode)
                        Console.WriteLine(inMsg.ReadString());
                        break;
                }
            }
        }

        void Process(NetIncomingMessage inMsg)
        {
            // Read packet type
            PacketType packetType = (PacketType) inMsg.ReadUInt16();
#if DEBUG
            Console.WriteLine("type: " + packetType);
#endif
            switch (packetType)
            {
                case PacketType.ClaimSquare:
                    // Parse parameters
                    string playerName = inMsg.ReadString();
                    Player pl = (from p in players where playerName == p.Name select p).Single();
                    int x = inMsg.ReadInt32();
                    int y = inMsg.ReadInt32();
                    // Claim square
                    grid.ClaimSquare(new Point(x, y), pl);
                    break;

                case PacketType.EndGame:
                    EndGame();
                    gameActive = false;
                    break;

                case PacketType.PlayerConnected:
                    // Add player to players
                    Player newPlayer = ByteSerializer.ByteArrayToObject<Player>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    players.Add(newPlayer);
                    break;

                case PacketType.PlayerDisconnected:
                    // Add player to players
                    Player player = ByteSerializer.ByteArrayToObject<Player>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    players.Remove(player);
                    break;

                case PacketType.NextTurn:
                    string n = inMsg.ReadString();
                    Console.WriteLine($"It is {n}´s turn.");
                    if (localPlayer.Name == n)
                    {
                        Thread t = new Thread(LocalPlayerTurn) {Name = nameof(LocalPlayerTurn)};
                        t.Start();
                    }

                    break;

                case PacketType.FailedClaimSquare:
                    Thread tr = new Thread(LocalPlayerTurn) {Name = nameof(LocalPlayerTurn)};
                    tr.Start();
                    break;

                case PacketType.GridData:
                    grid = ByteSerializer.ByteArrayToObject<Grid>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    action += confcam;
                    break;

                case PacketType.StartGame:
                    gameActive = true;
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(PacketType), "unknown packet type");
            }
        }

        void ListFoundServers()
        {
            foreach (KeyValuePair<IPEndPoint, FoundServer> valuePair in gameServers)
            {
                FoundServer fs = valuePair.Value;

                Console.WriteLine($"Server {valuePair.Key}:");
                Console.WriteLine($" Grid {fs.gridSizeX} * {fs.gridSizeY}");
                Console.WriteLine($" {fs.maxPlayers} Max players, {fs.minPlayers} Min players");
                Console.WriteLine($" {fs.currentPlayers} Current players");
            }
        }

        void LocalPlayerTurn()
        {
            MouseState previousMouseState = Mouse.GetState();
            while (true)
            {
                // Update Mouse
                MouseState mouseState = Mouse.GetState();

                // Place marker on mouse left click
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (previousMouseState.LeftButton == ButtonState.Released)
                    {
                        // Get the position of the mouse in the world
                        Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                        // Get the square that contains that position
                        Point? sq = grid.SquareContains(mouseWorldPos, gridLayout);
                        // If that square is valid, then claim it
                        if (sq != null)
                        {
                            // If the claim was successful
                            if (grid.ClaimSquare((Point) sq, ActivePlayer))
                            {
                                // Send claim square message
                                NetOutgoingMessage outMsg = client.CreateClaimSquareMessage(sq.Value.X, sq.Value.Y);
                                client.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
                                return;
                            }
                        }
                    }
                }

                previousMouseState = mouseState;
            }
        }


        enum Commands
        {
            ListServers,
            Connect,
            Discover,
            Help,
            Commands
        }
    }

    [Serializable]
    public class FoundServer // Q: use struct instead?
    {
        public readonly int currentPlayers;
        public readonly int gridSizeX;
        public readonly int gridSizeY;
        public readonly int maxPlayers;
        public readonly int minPlayers;

        public FoundServer(int gridSizeX, int gridSizeY, int maxPlayers, int minPlayers, int currentPlayers)
        {
            this.gridSizeX = gridSizeX;
            this.gridSizeY = gridSizeY;
            this.maxPlayers = maxPlayers;
            this.minPlayers = minPlayers;
            this.currentPlayers = currentPlayers;
        }
    }
}