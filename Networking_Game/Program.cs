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

            // Create Console thread
            ConsoleThread = new Thread(() =>
            {
                ConsoleManager.Initialize();
                ConsoleManager.Run();
            });

            // Initialize Console
            ConsoleThread.Name = nameof(ConsoleThread);
            ConsoleThread.Start();

            // Wait
            Thread.Sleep(100);

            // Initialize game
            GameThread.Name = nameof(GameThread);
            GameThread.Start();

        }

        public static void Restart()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string pid = currentProcess.Id.ToString();
            string path = @"Networking_Game";

            // Start ApplicationRestartHelper process
            Process.Start(@"ProcessRestartHelper", pid + " " + path);

            Exit();
        }

        public static void Exit()
        {
            Process.GetCurrentProcess().Kill();
        }
    }
#endif
}
