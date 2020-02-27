using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game.P2P
{
    /// <summary>
    ///     The Peer-to-Peer version of the game
    /// </summary>
    public class GamePeer : GameCore
    {
        public const string AppId = "NetworkingGamePeer";
        public static readonly int Port = 14243;
        public static readonly IPEndPoint HostIdentIP = new IPEndPoint(0, 1000);
        readonly NetPeer peer;
        Dictionary<IPEndPoint, FoundGame> DiscoveredHosts;
        bool gameActive, isHost;
        Player localPlayer;
        Thread messageHandlerThread, gameActivityThread;
        List<PlayerConnection> peers;
        int selfTurnIndex;

        public GamePeer()
        {
            peers = new List<PlayerConnection>();
            // Create peer
            NetPeerConfiguration config = new NetPeerConfiguration(AppId);
            // Select first available port
            Process cp = System.Diagnostics.Process.GetCurrentProcess();
            Process[] theProcesses = System.Diagnostics.Process.GetProcessesByName(cp.ProcessName);
            config.Port = Port + theProcesses.Length - 1; //BUG: if a process is closed after assigning port 
            // Enable discovery
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.AcceptIncomingConnections = true;
            config.AutoFlushSendQueue = true;
            peer = new NetPeer(config);
            peer.Start();

            // Start MessageHandlerThread
            StartMessageReaderThread();
        }


        public void ConnectToPeer(IPEndPoint endPoint)
        {
            // Create player data
            byte[] arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
            NetOutgoingMessage om = peer.CreateMessage(arrPlayer.Length);
            om.Write(arrPlayer);
            // Connect to server
            peer.Connect(endPoint.Address.ToString(), endPoint.Port);
        }

        protected override void Initialize()
        {
            // Create player
            //localPlayer = Player.GetPlayerSettingsInput();
            //players.Add(localPlayer);
            // Create Cached version of localPlayer as byte array 
            // byte[] TEMP = ByteSerializer.ObjectToByteArray(localPlayer); TODO: Cache byte array localPlayer

            gameActivityThread = new Thread(() =>
            {
                while (true) ActivityCheck();
            }) {Name = nameof(gameActivityThread)};
            gameActivityThread.Start();

            action += WaitForGameActive;
            base.Initialize();
        }

        static IPEndPoint GetIPAddress()
        {
            IPAddress ip;
            int port;

            // Request server ip from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input IP adress: ", false);
                // Parse input
                if (IPAddress.TryParse(input, out ip)) break;
                Console.WriteLine($"{input} is not a valid IP adress.");
            }

            // Request server port from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input port: ", false);
                // parse input
                if (int.TryParse(input, out port)) break;
                Console.WriteLine($"{input} is not a valid port.");
            }

            return new IPEndPoint(ip, port);
        }

        void ReadMessage()
        {
            NetIncomingMessage inMsg;
            if ((inMsg = peer.ReadMessage()) != null)
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
                        try
                        {
                            // Parse game data
                            FoundGame fg = ByteSerializer.ByteArrayToObject<FoundGame>(inMsg.Data);
                            // skip if game is already discovered / up to date
                            if (DiscoveredHosts.Contains(new KeyValuePair<IPEndPoint, FoundGame>(inMsg.SenderEndPoint, fg))) break;
                            // Add server to list
                            DiscoveredHosts.Add(inMsg.SenderEndPoint, fg);
                            Console.WriteLine($"Discovered host at: {inMsg.SenderEndPoint}");
                        }
                        catch { }

                        break;

                    case NetIncomingMessageType.DiscoveryRequest:
                        if (isHost && !gameActive) peer.SendDiscoveryResponse(PacketFactory.CreateFoundGameMessage(peer, FoundGame.CreateFromGrid(grid, peer.ConnectionsCount + 1)), inMsg.SenderEndPoint);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        if (isHost)
                        {
                            switch (inMsg.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Connected:
                                    if (isHost)
                                    {
                                        PlayerConnection np = new PlayerConnection(Player.GetRandomisedPlayer(), inMsg.SenderConnection.RemoteEndPoint);
                                        peers.Add(np);
                                        players.Add(np.player);
                                    }

                                    Console.WriteLine($"{inMsg.SenderConnection.RemoteEndPoint} {inMsg.SenderConnection.Status}");
                                    Console.WriteLine($"{peers.Count + 1} players currently connected");
                                    break;

                                case NetConnectionStatus.Disconnected:
                                    if (isHost)
                                    {
                                        PlayerConnection p = (from peer in peers where Equals(inMsg.SenderConnection.RemoteEndPoint, peer.ipEndPoint) select peer).Single();
                                        peers.Remove(p);
                                        players.Remove(p.player);
                                    }

                                    Console.WriteLine($"{inMsg.SenderConnection.RemoteEndPoint} {inMsg.SenderConnection.Status}");
                                    Console.WriteLine($"{peers.Count + 1} players currently connected");
                                    break;
                            }
                        }
                        else
                        {
                            switch (inMsg.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Connected:
                                case NetConnectionStatus.Disconnected:
                                    Console.WriteLine($"{inMsg.SenderConnection.RemoteEndPoint} {inMsg.SenderConnection.Status}");
                                    Console.WriteLine($"{peer.Connections.Count + 1} players currently connected");
                                    break;
                            }
                        }

                        break;

                    case NetIncomingMessageType.ConnectionApproval:
                        if (isHost)
                        {
                            if (gameActive)
                            {
                                inMsg.SenderConnection.Deny("Game already started");
                                break;
                            }

                            // not max players
                            if (grid.maxPlayers >= peer.ConnectionsCount + 1) inMsg.SenderConnection.Approve();
                            else inMsg.SenderConnection.Deny("Game is full");
                        }
                        else inMsg.SenderConnection.Approve(); // TODO: Validate with gameID

                        break;

                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                        Console.WriteLine(inMsg.ReadString());
                        break;

                    default: throw new ArgumentOutOfRangeException();
                }

                peer.Recycle(inMsg);
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
                    // Start next turn
                    NextTurn();
                    CheckLocalTurn();
                    break;

                case PacketType.PlayerConnected:
                    // connect
                    PlayerConnection newPlayer = ByteSerializer.ByteArrayToObject<PlayerConnection>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    peer.Connect(newPlayer.ipEndPoint);
                    break;

                case PacketType.PlayerDisconnected:
                    // disconnect
                    PlayerConnection player = ByteSerializer.ByteArrayToObject<PlayerConnection>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    (from connection in peer.Connections where Equals(connection.RemoteEndPoint, player.ipEndPoint) select connection).Single().Disconnect("BYE");
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

                case PacketType.Ready:
                    // Set player to ready
                    peers.Find(p => Equals(p.ipEndPoint, inMsg.SenderConnection.RemoteEndPoint)).isReady = true;

                    // Start game if all players are ready
                    if (StartGame()) Console.WriteLine("Starting game.");
                    break;

                case PacketType.PlayerAssignment:
                    // Read data
                    int numberOfBytes = inMsg.ReadInt32();
                    List<PlayerConnection> newPlayers = ByteSerializer.ByteArrayToObject<List<PlayerConnection>>(inMsg.ReadBytes(numberOfBytes));
                    Grid newGrid = ByteSerializer.ByteArrayToObject<Grid>(inMsg.ReadBytes(inMsg.Data.Length - inMsg.PositionInBytes));
                    // Set new grid
                    grid = newGrid;
                    action += confcam;
                    // set players
                    players = new List<Player>(newPlayers.Count);
                    peers = new List<PlayerConnection>(newPlayers.Count - 1);
                    for (int i = 0; i < newPlayers.Count; i++)
                    {
                        try
                        {
                            // Add peers, not self or host
                            //find a corresponding connection
                            (from connection in peer.Connections where Equals(newPlayers[i].ipEndPoint, connection.RemoteEndPoint) select connection).Single();

                            peers.Add(newPlayers[i]);
                            players.Add(newPlayers[i].player);
                        }
                        catch (Exception e)
                        {
                            // Sync failed, found invalid/no player/connection
                            // Check host
                            if (Equals(newPlayers[i].ipEndPoint, HostIdentIP))
                            {
                                players.Add(newPlayers[i].player);
                                peers.Add(new PlayerConnection(newPlayers[i].player, peer.Connections[0].RemoteEndPoint));
                                continue;
                            }

                            // Check self
                            string pip = NetworkHelper.GetPublicIP();
                            string iip = NetworkHelper.GetLocalIPAddress();
                            if (Equals(newPlayers[i].ipEndPoint, new IPEndPoint(IPAddress.Parse(pip), peer.Port)) || Equals(newPlayers[i].ipEndPoint, new IPEndPoint(IPAddress.Parse(iip), peer.Port)))
                            {
                                localPlayer = newPlayers[i].player;
                                selfTurnIndex = i;
                                players.Add(newPlayers[i].player);
                                continue;
                            }

                            // Not self either
                            Console.WriteLine(e);
                            throw;
                        }
                    }

                    StartGame();

                    break;

                default: throw new ArgumentOutOfRangeException(nameof(PacketType), "unknown packet type");
            }
        }

        void confcam()
        {
            ConfigureCamera();
            action -= confcam;
        }

        void WaitForGameActive()
        {
            while (!gameActive) Thread.Sleep(333);

            action -= WaitForGameActive;
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
                        case Commands.List:
                            ListFoundHosts();
                            break;

                        case Commands.Join:
                            if (isHost) Console.WriteLine("host cannot join other hosts");
                            else ConnectToHost(GetIPAddress());
                            break;

                        case Commands.Discover:
                            DiscoverHosts();
                            break;

                        case Commands.Help:
                        case Commands.Commands:
                            // List commands
                            Console.WriteLine("Commands:");
                            foreach (string c in Enum.GetNames(typeof(Commands))) Console.WriteLine(c);
                            break;

                        //case Commands.Ready:
                        //    if (ready) Console.WriteLine("You are already ready.");
                        //    else
                        //    {
                        //        ready = true;
                        //        Console.WriteLine("Ready!");
                        //    }
                        //    if (StartGame()) Console.WriteLine("Starting game.");
                        //    else Console.WriteLine("Waiting for other players.");
                        //    break;

                        case Commands.Create:
                            if (isHost) break;
                            // Create grid
                            grid = Grid.GetGameSettingsInput();
                            action += confcam;
                            // Create local player TODO: remake 
                            selfTurnIndex = 0;
                            localPlayer = Player.GetRandomisedPlayer();
                            players.Add(localPlayer);
                            isHost = true;
                            Console.WriteLine("hosting game");
                            break;

                        case Commands.Start:
                            if (!isHost) break;
                            if (gameActive) break;
                            if (peer.Connections.Count + 1 < grid.minPlayers)
                            {
                                Console.WriteLine("Not enough players to start");
                                break;
                            }

                            peer.SendMessage(PacketFactory.CreatePlayerAssignmentMessage(peer, peers, localPlayer, grid), peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
                            StartGame();
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

        bool CheckLocalTurn()
        {
            // If there are still squares to fill
            if (turnNumber < grid.Squares.Length - 1)
            {
                // Start next turn
                if (selfTurnIndex == activePlayerIndex)
                {
                    Thread t = new Thread(LocalPlayerTurn) { Name = nameof(LocalPlayerTurn) };
                    t.Start();
                    // Start next turn
                    NextTurn();
                    return true;
                }
            }
            else
            {
                // End game
                EndGame();
            }

            return false;
        }

        /// <summary>
        ///     starts game if all players are ready and minPlayers is fulfilled
        /// </summary>
        /// <returns></returns>
        bool StartGame()
        {
            if (peers.TrueForAll(p => p.isReady) && peer.Connections.Count + 1 >= grid.minPlayers && !gameActive)
            {
                gameActive = true;

                CheckLocalTurn();

                return true;
            }

            return false;
        }

        void StartMessageReaderThread()
        {
            messageHandlerThread = new Thread(() =>
            {
                while (true) ReadMessage();
            }) {Name = nameof(ReadMessage)};
            messageHandlerThread.Start();
        }

        /// <summary>
        ///     Attempts to connect to a host
        /// </summary>
        void ConnectToHost(IPEndPoint endPoint)
        {
            // Create player data
            //byte[] arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
            //NetOutgoingMessage om = peer.CreateMessage(arrPlayer.Length);
            //om.Write(arrPlayer);
            // Connect to server
            peer.Connect(endPoint.Address.ToString(), endPoint.Port);
        }

        void ListFoundHosts()
        {
            foreach (KeyValuePair<IPEndPoint, FoundGame> valuePair in DiscoveredHosts)
            {
                FoundGame fs = valuePair.Value;

                Console.WriteLine($"Host {valuePair.Key}:");
                Console.WriteLine($" Grid {fs.gridSizeX} * {fs.gridSizeY}");
                Console.WriteLine($" {fs.maxPlayers} Max players, {fs.minPlayers} Min players");
                Console.WriteLine($" {fs.currentPlayers} Current players");
            }
        }

        void DiscoverHosts()
        {
            DiscoveredHosts = new Dictionary<IPEndPoint, FoundGame>();
            // Send discovery signal on known ports
            Console.Write("Discovering hosts");
            for (int i = 0; i < 6; i++) peer.DiscoverLocalPeers(Port + i);

            // Wait for responses
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(500);
                Console.Write(".");
            }

            Console.WriteLine($"\nFound {DiscoveredHosts.Count} hosts.");
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
                                NetOutgoingMessage outMsg = PacketFactory.CreateClaimSquareMessage(peer, localPlayer, sq.Value.X, sq.Value.Y);
                                peer.SendMessage(outMsg, peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);

                                return;
                            }
                        }
                    }
                }

                // Update previous mouse state
                previousMouseState = mouseState;
            }
        }

        enum Commands
        {
            List,
            Join,
            Create,
            Start,
            Discover,
            Help,
            Commands
        }
    }

    [Serializable]
    public class FoundGame
    {
        public readonly int currentPlayers;
        public readonly int gridSizeX;
        public readonly int gridSizeY;
        public readonly int maxPlayers;

        public readonly int minPlayers;
        //public DateTime LastUpdate;

        public FoundGame(int gridSizeX, int gridSizeY, int maxPlayers, int minPlayers, int currentPlayers)
        {
            this.gridSizeX = gridSizeX;
            this.gridSizeY = gridSizeY;
            this.maxPlayers = maxPlayers;
            this.minPlayers = minPlayers;
            this.currentPlayers = currentPlayers;
            //LastUpdate = DateTime.Now;
        }

        public static FoundGame CreateFromGrid(Grid grid, int currentPlayers) { return new FoundGame(grid.sizeX, grid.sizeY, grid.maxPlayers, grid.minPlayers, currentPlayers); }

        // TODO: ADD LastUpdate in order to implement timeout for discovery
        //public override bool Equals(object obj) { return base.Equals(obj); }
        //public override int GetHashCode() { return base.GetHashCode(); }
    }
}