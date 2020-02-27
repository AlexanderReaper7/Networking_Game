using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CSharp;
using Microsoft.Xna.Framework;
using Tools_XNA_dotNET_Framework;
using Color = System.Drawing.Color;
using Colorful;
using Networking_Game.ClientServer;
using Networking_Game.Local;
using Networking_Game.P2P;
using Console = Colorful.Console;

namespace Networking_Game
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        public static Thread GameThread;
        public static Game Game;
        public static GameServer Server;

        public const string AppId = "NetworkingGame";
        public const int DefaultPort = 14242;

        private enum StartCommand
        {
            Local,
            P2P,
            Client,
            Server,
            ClientAndServer,
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Start console
            ConsoleManager.Start();
            ConsoleManager.Initialize();

            // Wait for console to get ready TODO: Actually check for when ConsoleManager is ready
            Thread.Sleep(1000);

            // Ask what game to start
            StartCommand command;
            while (true)
            {
                // Write available commands
                Console.WriteLine("Pick one of the following commands", Color.White);
                foreach (string commandName in Enum.GetNames(typeof(StartCommand))) // TODO: Also number the commands
                {
                    Console.WriteLine(commandName, Color.White);
                }
                // Get and parse input
                if (!Enum.TryParse(ConsoleManager.GetPriorityInput(), true, out command))
                {
                    Console.WriteLine("Input invalid, try again.", Color.Red);
                }
                // Break loop on successful parse
                else break;
            }

            // Start game
            switch (command)
            {
                case StartCommand.Local:
                    StartLocal();
                    break;
                case StartCommand.P2P:
                    StartPeerToPeer();
                    break;
                case StartCommand.Client:
                    StartClient();
                    break;
                case StartCommand.Server:
                    StartServer();
                    break;
                case StartCommand.ClientAndServer:
                    StartClientAndServer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Starts the local game version
        /// </summary>
        private static void StartLocal()
        {
            // Create local game thread
            GameThread = new Thread(() =>
            {
                Game = new GameLocal();
                Game.Run();
            });

            GameThread.Name = nameof(GameLocal);
            GameThread.Start();
        }

        /// <summary>
        /// Starts the peer to peer version
        /// </summary>
        private static void StartPeerToPeer()
        {
            GameThread = new Thread(() =>
            {
                Game = new GamePeer();
                Game.Run();
            });

            GameThread.Name = nameof(GamePeer);
            GameThread.Start();
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        private static void StartServer()
        {
            GameThread = new Thread(() =>
            {
                //Server = new GameServer();
                //Server.StartServer();
                Server.Run();
            });

            GameThread.Name = nameof(GameServer);
            GameThread.Start();
        }

        /// <summary>
        /// Starts the client
        /// </summary>
        private static void StartClient()
        {
            GameThread = new Thread(() =>
            {
                Game = new GameClient();
                Game.Run();
            });

            GameThread.Name = nameof(GameClient);
            GameThread.Start();
        }

        /// <summary>
        /// Starts both a client and a server
        /// </summary>
        private static void StartClientAndServer()
        {
            throw new NotImplementedException();
        }

        public static void Restart()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string pid = currentProcess.Id.ToString();
            string path = @"Networking_Game";

            // Start ApplicationRestartHelper process
            Process.Start(@"ProcessRestartHelper", pid);

            Exit();
        }

        public static void Exit()
        {
            Process.GetCurrentProcess().Kill();
        }
    }
#endif
}
