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
    class Client
    {
        private NetClient client;

        public void StartClient()
        {
            var config = new NetPeerConfiguration(Program.AppId);
            config.AutoFlushSendQueue = false;
            client = new NetClient(config);
            client.Start();

            string ip = "localhost";
            int port = Program.DefaultPort;
            client.Connect(ip, port);
        }

        public void SendMessage(string text)
        {
            NetOutgoingMessage message = client.CreateMessage(text);

            client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            client.FlushSendQueue();
        }

        public void Disconnect()
        {
            client.Disconnect("Bye");
        }
    }



    /// <summary>
    /// The client for the client-server version of the game
    /// </summary>
    public class GameClient : GameCore
    {
        private NetClient client;

        public GameClient()
        {
            StartClient("localhost");
        }

        public void StartClient(string ip, int port = Program.DefaultPort)
        {
            NetPeerConfiguration config = new NetPeerConfiguration(Program.AppId);
            config.AutoFlushSendQueue = false;
            client = new NetClient(config);
            client.Start();

            client.Connect(ip, port);
            client.
        }

        private void SendClaimSquare()
        {
            NetOutgoingMessage message = client.CreateMessage(text);
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
