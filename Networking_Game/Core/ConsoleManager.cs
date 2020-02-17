using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Color = System.Drawing.Color;
using Console = Colorful.Console;

namespace Networking_Game
{
    // TODO Write documentation / comments
    // TODO Move to Tools
    public static class ConsoleManager
    {
        const int SWP_NOZORDER = 0x4;
        const int SWP_NOACTIVATE = 0x10;

        /// <summary>
        /// Allocates a console window
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern bool AllocConsole();

        /// <summary>
        /// Gets the handle for the console window
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern IntPtr GetConsoleWindow();


        [DllImport("User32")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        /// <summary>
        /// Sets the window´s position and size
        /// </summary>
        /// <param name="hWnd">the windows handle</param>
        /// <param name="hWndInsertAfter"></param>
        /// <param name="x">position x</param>
        /// <param name="y">position y</param>
        /// <param name="cx">size x</param>
        /// <param name="cy">size y</param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport("user32")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        /// <summary>
        /// Gets the size of the primary monitor
        /// </summary>
        internal static Point ScreenSize => new Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

        #region active commands

        /// <summary>
        /// Defines a command with a name and code
        /// </summary>
        public interface ICommand
        {
            /// <summary>
            /// The string that identifies the command
            /// </summary>
            string Key { get; }
            /// <summary>
            /// The code the command executes
            /// </summary>
            Action Code { get; }
        }

        private static Dictionary<string, Action> activeCommands = new Dictionary<string, Action>();

        public static void AddCommand(ICommand command)
        {
            activeCommands.Add(command.Key, command.Code);
        }

        public static void AddCommand(string key, Action code)
        {
            activeCommands.Add(key, code);
        }

        public static void RemoveCommand(ICommand command)
        {
            activeCommands.Remove(command.Key);
        }

        public static void RemoveCommand(string key)
        {
            activeCommands.Remove(key);
        }
        
        #endregion

        /// <summary>
        /// Is the console loop running
        /// </summary>
        public static bool IsRunning { get; private set; }

        private static Thread consoleThread;

        private const string ConsoleThreadName = "ConsoleManager_Worker";


        /// <summary>
        /// Position and size of the console window
        /// </summary>
        public static Rectangle ConsoleWindow { get; private set; }

        /// <summary>
        /// Sets the console window location and size in pixels
        /// </summary>
        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            if (SetWindowPos(GetConsoleWindow(), IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE))
            {
                // Success
                ConsoleWindow = new Rectangle(x,y, width, height);
            }
            else
            {
                // Fail
                Console.WriteLine("Error, failed setting console window size and position.", Color.Red);
            }
        }
        // TODO: move to gamecore
        public static void Initialize() 
        {
            Console.WriteLine("Initializing console", Color.Gray);
            // Set console size and move to top left
            SetWindowPosition(-8, -1,  MathHelper.Clamp((ScreenSize.X) / 3, 700, 1000) , ScreenSize.Y +8);
        }

        public static bool IsPriorityInput = false; // TODO: Create a waiting que instead
        private static string priorityInput = null;

        /// <summary>
        /// Gets input from console
        /// </summary>
        /// <returns>The input from user</returns>
        /// <exception cref="Exception">Priority input is already Active</exception>
        public static string GetPriorityInput()
        {
            if (IsPriorityInput) throw new Exception("Priority input is already Active");
            if (!IsRunning) throw new Exception("Console loop not running");

            // Enable input
            IsPriorityInput = true;
            // Wait for input
            while (priorityInput == null) { Thread.Sleep(300); }

            string temp = priorityInput;
            priorityInput = null;
            return temp;
        }

        /// <summary>
        /// Waits until PriorityInput is free then writes prompt and gets console input
        /// </summary>
        /// <param name="prompt">The string to write</param>
        /// <param name="useWriteLine">Use Console.WriteLine() or .Write()</param>
        /// <returns></returns>
        public static string WaitGetPriorityInput(string prompt, bool useWriteLine = true)
        {
            // Wait until IsPriorityInput is false
            while (IsPriorityInput && !IsRunning)
            {
                Thread.Sleep(500);
            }
            // Write prompt
            if (useWriteLine) Console.WriteLine(prompt);
            else Console.Write(prompt);

            // Get input
            return GetPriorityInput();
        }

        public static void Start()
        {
            AllocConsole();

            consoleThread = new Thread(Run);
            consoleThread.Name = ConsoleThreadName;
            consoleThread.Start();
        }

        /// <summary>
        /// Starts the console loop
        /// </summary>
        /// <exception cref="Exception">Console loop is already running</exception>
        private static void Run()
        {
            VerifyConsoleThread();
            if (IsRunning) throw new Exception("The console loop is already running");
            IsRunning = true;
            // Console loop
            while (IsRunning)
            {
                // Get input
                string input = Console.ReadLine();
                // Split into words separated by Space
                string[] processedInput = input.Split(' ');
                string commandString = processedInput[0].ToLower();

                // Check standard commands
                switch (commandString)
                {
                    case "exit":
                    {
                        Program.Exit();
                        Stop();
                        return;
                    }
                    case "restart":
                    {
                        Program.Restart();
                        continue;
                    }
                }

                // If priority input is active, pass string to outside and restart loop
                if (IsPriorityInput)
                {
                    priorityInput = input;
                    IsPriorityInput = false;
                    continue;
                }

                // Check Active commands
                foreach (KeyValuePair<string, Action> command in activeCommands)
                {
                    if (commandString == command.Key)
                    {
                        command.Value.Invoke();
                        break;
                    }
                }

                // If no command was found
                Console.WriteLine($"Unrecognized command: {commandString}");
            }

            Stop();
        }

        /// <summary>
        /// Stops the console loop
        /// </summary>
        /// <exception cref="Exception"></exception>
        private static void Stop()
        {
            if (!IsRunning) throw new Exception("The console loop is not running");
            if (!consoleThread.IsAlive) throw new Exception("The console thread is not alive");
            consoleThread.Abort();
            IsRunning = false;
        }

        /// <summary>
        /// Verifies the executor is the ConsoleManager thread
        /// </summary>
        private static void VerifyConsoleThread()
        {
            Thread ct = Thread.CurrentThread;
            if (Thread.CurrentThread != consoleThread)
                throw new ThreadStateException($"Executing on wrong thread! Should be ConsoleManager thread (is {ct.Name} mId {ct.ManagedThreadId}");
        }
    }
}
