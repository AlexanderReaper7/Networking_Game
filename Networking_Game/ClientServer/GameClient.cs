using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public GameClient() : base()
        {
            // Create player
            GetPlayerSettingsInput(out localPlayer);
            // Request server ip from user
            System.Console.WriteLine("Server IP: ");
            string ip = ConsoleManager.GetPriorityInput();
            // Connect to game
            StartClient(ip);
            // Send player
            
            // if player conflict, make new player
            // Receive game information
            // Send player
        }

        private void StartClient(string ip, int port = Program.DefaultPort)
        {
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.AutoFlushSendQueue = false;
            client = new NetClient(config);
            client.Start(); 

            client.Connect(ip, port, CreateJoinGameMessage());
        }

        public NetOutgoingMessage CreateJoinGameMessage()
        {
            NetOutgoingMessage message = client.CreateMessage();
            message.WriteAllFields(localPlayer);

            return message;
        }

        public void SendMessage(string text)
        {
            NetOutgoingMessage message = client.CreateMessage(text);
            client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            client.FlushSendQueue();

        }
    

        private void SendClaimSquare()
        {
            NetOutgoingMessage message = client.CreateMessage();
            client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            client.FlushSendQueue();
        }

        protected override void PreUpdate()
        {
            base.PreUpdate();
        }

        protected override void PostUpdate()
        {

             
            base.PostUpdate();
        }

        public void Disconnect()
        {
            client.Disconnect("Bye");
        }
    }
}
