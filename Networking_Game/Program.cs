using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Runtime.CompilerServices;
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
        const int SWP_NOZORDER = 0x4;
        const int SWP_NOACTIVATE = 0x10;

        [DllImport("kernel32")]
        private static extern bool AllocConsole();
        [DllImport("kernel32")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("user32")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);
        [DllImport("kernel32")]
        private static extern bool FreeConsole();
        public static IntPtr Handle => GetConsoleWindow();

        /// <summary>
        /// Sets the console window location and size in pixels
        /// </summary>
        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }
        private static Thread gameThread;

        static Game1 game;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Create console
            //FreeConsole();
            AllocConsole();
            //Console.SetWindowPosition(0, 0);

            // Create game
            gameThread = new Thread(new ThreadStart(() =>
            {
                game = new Game1();
                game.Run();
            }));
            gameThread.Start();

            Console.WriteLine("Compiling...", Color.Gray);
            DynamicCompiling.CompileAndRun(@"using System; class Program{[STAThread] private static void Main(){Console.WriteLine(""HELP"");}}");
            Thread.Sleep(1000);

            // Set focus to console
            Console.WriteLine("Setting Console to active window: " + (SetForegroundWindow(GetConsoleWindow()) ? "Success" : "Failed"));

            // Move console to top left
            Console.WriteLine("Moving console", Color.Gray);
            SetWindowPosition(0,0, 860,1080);
            // Console loop
            StartLoop:
            while (true)
            {
                // Get input
                string input = Console.ReadLine();

                // Check custom commands
                foreach (KeyValuePair<string, Commands.Code> keyValuePair in Commands.Dictionary)
                {
                    if (input == keyValuePair.Key)
                    {
                        keyValuePair.Value.Invoke();
                        goto StartLoop;
                    }
                }

                // Check standard commands
                switch (input)
                {
#if DEBUG
                    case "hello":
                    {
                        Console.WriteLine("Hello world!");
                        break;
                    }
#endif
                    case "exit":
                    {
                        game.Exit();
                        return;
                    }
                         
                    default:
                    {
                        Console.WriteLine($"Unrecognized command: {input}");
                        break;
                    }
                }
            }

        }
    }
#endif
}
