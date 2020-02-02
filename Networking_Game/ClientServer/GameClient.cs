using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Tools_XNA_dotNET_Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Networking_Game.Local;
using Color = System.Drawing.Color;
using Console = Colorful.Console;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Networking_Game.ClientServer
{
    /// <summary>
    /// The client for the client-server version of the game
    /// </summary>
    public class GameClient : GameCore
    {
        private Player localPlayer; 
        private NetClient client;
        private List<GameServerArguments> gameServers;

        protected override void Initialize()
        {
            // Create player
            localPlayer = Player.GetPlayerSettingsInput();
            players.Add(localPlayer);
            // Create Cached version of localPlayer as byte array 
            // byte[] TEMP = ByteSerializer.ObjectToByteArray(localPlayer); TODO: Cache byte array localPlayer

            StartClient();

            // TODO: Remake client connection
            // Discover servers


            base.Initialize();
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
        }

        /// <summary>
        /// Uses network discovery to find servers
        /// </summary>
        private void DiscoverServers()
        {
            // Discover on default port
            Console.WriteLine("Discovering Servers...");
            client.DiscoverLocalPeers(Program.DefaultPort);

            // Check for answers after 1.5 seconds
            Thread.Sleep(1500);
            System.Console.WriteLine($"Found {gameServers.Count} server(s)");

            // Ask user if to continue to discover on custom port
            bool d = true;
            do
            {
                string s = ConsoleManager.WaitGetPriorityInput("Input server port to continue discovering or leave empty to stop");
                if (s.Length > 0)
                {
                    // Parse input
                    if (int.TryParse(s, out int p))
                    {
                        Console.WriteLine("Discovering Servers...");
                        client.DiscoverLocalPeers(p);
                    }
                    else System.Console.WriteLine("input is not a valid port");
                }
                else
                {
                    d = false;
                }
            } 
            while (d);
        }

        private void DirectConnectServer(IPEndPoint endPoint)
        {
            // Create player data
            var arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
            var om = client.CreateMessage(arrPlayer.Length);
            om.Write(arrPlayer);
            // Connect to server
            client.Connect(endPoint.Address.ToString(), endPoint.Port, om);
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
            do s = ConsoleManager.WaitGetPriorityInput("Input client port: ", false);
            while (!int.TryParse(s, out port));
            
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
                if (IPAddress.TryParse(input, out ip))
                {
                    break;
                }
                else
                {
                    System.Console.WriteLine($"{input} is not a valid IP adress.");
                }
            }
            // Request server port from user
            while (true)
            {
                // Get user input
                string input = ConsoleManager.WaitGetPriorityInput("Input server port: ", false);
                // parse input
                if (int.TryParse(input, out port))
                {
                    break;
                }
                else
                {
                    System.Console.WriteLine($"{input} is not a valid port.");
                }
            }

            return new IPEndPoint(ip, port);
        }


        protected override void Update(GameTime gameTime)
        {
            // Read message
            CheckMessages();

            base.Update(gameTime);
        }

        private void CheckMessages()
        {
            NetIncomingMessage inMsg;
            while ((inMsg = client.ReadMessage()) != null)
            {
                switch (inMsg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        Proccess(inMsg);
                        break;

                    case NetIncomingMessageType.DiscoveryResponse:
                        // TODO: List server in discovered servers
                        gameServers.Add(new GameServerArguments());


                        // Instantly connect to server
                        //byte[] arrPlayer = ByteSerializer.ObjectToByteArray(localPlayer);
                        //NetOutgoingMessage hail = client.CreateMessage(arrPlayer.Length);
                        //hail.Write(arrPlayer);
                        //client.Connect(inMsg.SenderEndPoint, hail);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (inMsg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                System.Console.WriteLine($"connected to {inMsg.SenderConnection.RemoteEndPoint}");
                                break;
                            case NetConnectionStatus.Disconnecting:
                                System.Console.WriteLine($"Disconnecting from {inMsg.SenderConnection.RemoteEndPoint}");
                                break;
                            case NetConnectionStatus.Disconnected:
                                System.Console.WriteLine($"Disconnected from {inMsg.SenderConnection.RemoteEndPoint}");
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
        
        private void Proccess(NetIncomingMessage inMsg)
        {
            // Read packet type
            PacketType packetType = (PacketType) inMsg.ReadUInt16();

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
                        Thread t = new Thread(LocalPlayerTurn);
                        t.Start();
                    }
                    break;
                case PacketType.FailedClaimSquare:
                    Thread tr = new Thread(LocalPlayerTurn);
                    tr.Start();
                    break;
                case PacketType.GridData:
                    grid = ByteSerializer.ByteArrayToObject<Grid>(inMsg.ReadBytes(inMsg.Data.Length - 2));
                    ConfigureCamera();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(PacketType), "unknown packet type");
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
                            if (grid.ClaimSquare((Point)sq, ActivePlayer))
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
    }
}
