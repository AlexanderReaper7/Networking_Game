using System;
using System.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CSharp;
using Microsoft.Xna.Framework;
using Tools_XNA_dotNET_Framework;
using Color = System.Drawing.Color;
using Colorful;
using Console = Colorful.Console;

namespace Networking_Game
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {

        public static ConsoleManager ConsoleManager;
        public static Thread GameThread;
        public static Thread ConsoleThread;
        public static LocalGame Game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Create console
            ConsoleManager = new ConsoleManager();

            // Create Game thread
            GameThread = new Thread(() =>
            {
                Game = new LocalGame();
                Game.Run();
            });

            ConsoleThread = new Thread(() =>
            {
                ConsoleManager.Initialize();
                ConsoleManager.Run();
            });

            // Initialize game
            GameThread.Name = nameof(GameThread);
            GameThread.Start();
            // Wait for game
            while (Game == null) { }
            while (!Game.IsActive) { }

            // Initialize Console
            ConsoleThread.Name = nameof(ConsoleThread);
            ConsoleThread.Start();
        }


    }
#endif
}
