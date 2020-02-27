using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Networking_Game.ClientServer;
using Tools_XNA_dotNET_Framework;
using Console = Colorful.Console;

namespace Networking_Game
{
    class GameNetwork : GameCore
    {
        private Thread messageHandlerThread, gameActivityThread;
        private bool isMessageReadLoopRunning;
        private bool gameActive;
        private Player localPlayer;
        private Dictionary<IPEndPoint, FoundGame> gameServers;


        private enum Commands
        {
            ListServers,
            Connect,
            Discover,
            Help,
            Commands
        }

        protected override void Initialize()
        {
            // Create player
            localPlayer = Player.GetPlayerSettingsInput();
            players.Add(localPlayer);
            // Create Cached version of localPlayer as byte array 
            // byte[] TEMP = ByteSerializer.ObjectToByteArray(localPlayer); TODO: Cache byte array localPlayer

            StartClient();

            gameServers = new Dictionary<IPEndPoint, FoundGame>();
            Thread.Sleep(1000);
            // Discover servers
            DiscoverServers();
            gameActivityThread = new Thread(ActivityCheck) {Name = nameof(gameActivityThread)};
            gameActivityThread.Start();

            action += WaitForGameActive;
            DirectConnectServer(gameServers.Keys.ToArray()[0]);
            base.Initialize();
        }

        private void WaitForGameActive()
        {
            while (!gameActive) Thread.Sleep(333);

            action -= WaitForGameActive;
        }

        private void ActivityCheck()
        {
            // If no game is active
            if (!gameActive)
            {
                // Ask user for command
                string s = ConsoleManager.WaitGetPriorityInput("Input command: ", false);
                if (Enum.TryParse(s, true, out Commands command))
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
                            System.Console.WriteLine("Commands:");
                            foreach (string c in Enum.GetNames(typeof(Commands))) System.Console.WriteLine(c);
                            break;
                        default:
                            System.Console.WriteLine("Unrecognized command");
                            break;
                    }
                else System.Console.WriteLine("Unrecognized command");
            }
            else
            {
                Thread.Sleep(1000);
            }
        }

        private void StartClient()
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

        private void StartMessageReaderThread()
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
        /// Uses network discovery to find servers
        /// </summary>
        private void DiscoverServers()
        {
            // Discover on default port
            Console.WriteLine("Discovering Servers...");
            client.DiscoverLocalPeers(Program.DefaultPort);

            // Check for answers after x seconds
            Thread.Sleep(2000);
            System.Console.WriteLine($"Found {gameServers.Count} server(s)");

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
                        Console.WriteLine("Discovering Servers...");
                        client.DiscoverLocalPeers(p);
                        Thread.Sleep(1200);
                        System.Console.WriteLine($"Found {gameServers.Count} server(s)");
                    }
                    else
                    {
                        System.Console.WriteLine("input is not a valid port");
                    }
                }
                else
                {
                    break; // If input is empty stop discovery
                }
            } while (true);
        }


        /// <summary>
        /// Attempts to connect to a server
        /// </summary>
        private void DirectConnect(IPEndPoint endPoint)
        {
            // Create player data
            byte[] arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
            var om = peer.CreateMessage(arrPlayer.Length);
            om.Write(arrPlayer);
            // Connect to server
            peer.Connect(endPoint.Address.ToString(), endPoint.Port, om);
        }

        /// <summary>
        /// Gets client port from console input
        /// </summary>
        /// <returns></returns>
        private static int GetClientPort()
        {
            int port;
            string s;

            // Get client port
            do
            {
                s = ConsoleManager.WaitGetPriorityInput("Input client port: ", false);
            } while (!int.TryParse(s, out port));

            return port;
        }

        private static IPEndPoint GetServerAddress()
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
                else System.Console.WriteLine($"{input} is not a valid IP adress.");
            }

            // Request server port from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input server port: ", false);
                // parse input
                if (int.TryParse(input, out port)) break;
                else System.Console.WriteLine($"{input} is not a valid port.");
            }

            return new IPEndPoint(ip, port);
        }

        private void ReadMessages()
        {
            NetIncomingMessage inMsg;
            if ((inMsg = client.ReadMessage()) != null)
            {
#if DEBUG
                System.Console.WriteLine($"msg: {inMsg.MessageType}");
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
                        FoundGame fs = ByteSerializer.ByteArrayToObject<FoundGame>(inMsg.Data);
                        // Add server to list
                        gameServers.Add(inMsg.SenderEndPoint, fs);

                        System.Console.WriteLine($"Discovered server at: {inMsg.SenderEndPoint}");
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (inMsg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                            case NetConnectionStatus.Disconnecting:
                            case NetConnectionStatus.Disconnected:
                                System.Console.WriteLine($"{inMsg.SenderConnection.Status} from {inMsg.SenderConnection.RemoteEndPoint}");
                                break;
                        }

                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages (only received when compiled in DEBUG mode)
                        System.Console.WriteLine(inMsg.ReadString());
                        break;
                }
            }
        }

        private void Process(NetIncomingMessage inMsg)
        {
            // Read packet type
            PacketType packetType = (PacketType) inMsg.ReadUInt16();
#if DEBUG
            System.Console.WriteLine("type: " + packetType);
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
                    System.Console.WriteLine($"It is {n}´s turn.");
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
                    action += ConfigureCamera;
                    break;
                case PacketType.StartGame:
                    gameActive = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(PacketType), "unknown packet type");
            }
        }

        private void ListFoundServers()
        {
            foreach (KeyValuePair<IPEndPoint, FoundGame> valuePair in gameServers)
            {
                FoundGame fs = valuePair.Value;

                System.Console.WriteLine($"Server {valuePair.Key}:");
                System.Console.WriteLine($" Grid {fs.gridSizeX} * {fs.gridSizeY}");
                System.Console.WriteLine($" {fs.maxPlayers} Max players, {fs.minPlayers} Min players");
                System.Console.WriteLine($" {fs.currentPlayers} Current players");
            }
        }

        private void LocalPlayerTurn()
        {
            MouseState previousMouseState = Mouse.GetState();
            while (true)
            {
                // Update Mouse
                MouseState mouseState = Mouse.GetState();

                // Place marker on mouse left click
                if (mouseState.LeftButton == ButtonState.Pressed)
                    if (previousMouseState.LeftButton == ButtonState.Released)
                    {
                        // Get the position of the mouse in the world
                        Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                        // Get the square that contains that position
                        Point? sq = grid.SquareContains(mouseWorldPos, gridLayout);
                        // If that square is valid, then claim it
                        if (sq != null)
                            // If the claim was successful
                            if (grid.ClaimSquare((Point) sq, ActivePlayer))
                            {
                                // Send claim square message
                                NetOutgoingMessage outMsg = client.CreateClaimSquareMessage(sq.Value.X, sq.Value.Y);
                                client.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
                                return;
                            }
                    }

                previousMouseState = mouseState;
            }
        }
    }

    [Serializable]
    public class FoundGame // Q: use struct instead?
    {
        public readonly int gridSizeX;
        public readonly int gridSizeY;
        public readonly int maxPlayers;
        public readonly int minPlayers;
        public readonly int currentPlayers;

        public FoundGame(int gridSizeX, int gridSizeY, int maxPlayers, int minPlayers, int currentPlayers)
        {
            this.gridSizeX = gridSizeX;
            this.gridSizeY = gridSizeY;
            this.maxPlayers = maxPlayers;
            this.minPlayers = minPlayers;
            this.currentPlayers = currentPlayers;
        }
    }

}