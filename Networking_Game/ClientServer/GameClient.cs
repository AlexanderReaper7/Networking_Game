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
        bool activeGame = false;

        public GameClient() : base()
        {
        }

        protected override void Initialize()
        {
            StartClient();
            base.Initialize();
        }

        private void StartClient()
        {
            IPAddress ip;
            int port = Program.DefaultPort;
            while (true)
            {
                // Request server ip and port from user
                System.Console.WriteLine("Enter Server IP and port with format IP,port or leave blank to use local network discovery");
                var input = ConsoleManager.GetPriorityInput().Split(',');

                // connect to game
                switch (input.Length)
                {
                    case 0:
                        // use network discovery
                        System.Console.WriteLine("Using local peer discovery");
                        continue;
                    case 1:
                        // use IP and standard port
                        if (IPAddress.TryParse(input[0], out ip)) break;
                        port = Program.DefaultPort;
                        System.Console.WriteLine($"Connecting to {ip} on port {port}");
                        continue;
                    case 2:
                        // Use IP and port
                        if (!IPAddress.TryParse(input[0], out ip)) continue;
                        if (int.TryParse(input[1], out port)) break;
                        System.Console.WriteLine($"Connecting to {ip} on port {port}");
                        continue;
                    default:
                        System.Console.WriteLine("syntax error");
                        continue;
                }

                break;
            }


            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            client = new NetClient(config);
            client.Start(); 
            // Create player
            var arrPlayer = ByteSerializer.ObjectToByteArray(Player.GetPlayerSettingsInput());
            var om = client.CreateMessage(arrPlayer.Length);
            om.Write(arrPlayer);

            // Connect to server
            if (ip != null)
            {
                // use ip
                client.Connect(ip.ToString(), port, om);
            }
            else
            {
                // use discovery
                client.DiscoverLocalPeers(port);
            }
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
            if ((inMsg = client.ReadMessage()) != null)
            {
                switch (inMsg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        //InGameMessage = ServerMessage.ReadString();
                        //client.Connect(ServerMessage.SenderEndPoint);
                        //InGameMessage = "Connected to " + ServerMessage.SenderEndPoint.Address.ToString();
                        //if (thisPlayer == null)
                        //{
                        //    string ImageName = "Badges_" + Utility.NextRandom(0, Utility.PlayerTextures.Count - 1);
                        //    thisPlayer = new GamePlayer(this, client, Guid.NewGuid(), ImageName,
                        //                  new Vector2(Utility.NextRandom(100, GraphicsDevice.Viewport.Width - 100),
                        //                               Utility.NextRandom(100, GraphicsDevice.Viewport.Height - 100)));

                        //}

                        break;
                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        //switch (ServerMessage.SenderConnection.Status)
                        //{
                        //    /* .. */
                        //}
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        //InGameMessage = ServerMessage.ReadString();
                        break;

                        /* .. */

                        //InGameMessage = "unhandled message with type: "
                        //    + ServerMessage.MessageType.ToString();
                        break;
                }
            }
        }
    }
}
