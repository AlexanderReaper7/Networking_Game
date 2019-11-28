using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Color = System.Drawing.Color;
using Console = Colorful.Console;

namespace Networking_Game
{
    public class ConsoleManager //TODO write documentation / comments
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
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        private static IntPtr Handle => GetConsoleWindow();

        private static Point ScreenSize => new Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

        public interface ICommand
        {
            string Key { get; }
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

        /// <summary>
        /// Position and size of the console window
        /// </summary>
        public static Rectangle ConsoleWindow { get; private set; }

        public ConsoleManager()
        {
            AllocConsole();
        }

        /// <summary>
        /// Sets the console window location and size in pixels
        /// </summary>
        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            if (SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE))
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

        public void Initialize()
        {
            Console.WriteLine("Initializing console", Color.Gray);
            // Set console size and move to top left
            SetWindowPosition(-8, -1,  MathHelper.Clamp((ScreenSize.X) / 3, 700, 1000) , ScreenSize.Y +8);
        }

        public static bool IsPriorityInput = false;
        private static string priorityInput = null;

        /// <summary>
        /// Gets input from console
        /// </summary>
        /// <returns>The input from user</returns>
        public static string GetPriorityInput()   
        {
            if (IsPriorityInput)
            {
                throw new Exception("Priority input is already Active");
            }
            // enable input
            IsPriorityInput = true;
            // wait for input
            while (priorityInput == null) { }

            string temp = priorityInput;
            priorityInput = null;
            return temp;
        }

        /// <summary>
        /// Starts the console loop
        /// </summary>
        public void Run()
        {
            // Console loop
            while (true)
            {
                START_LOOP:
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
                        return;
                    }
                    case "restart":
                    {
                        Program.Restart();
                        goto START_LOOP;
                    }
                }

                // If priority input is active, pass string to outside and restart loop
                if (IsPriorityInput)
                {
                    priorityInput = input;
                    IsPriorityInput = false;
                    goto START_LOOP;
                }

                // Check Active commands
                foreach (KeyValuePair<string, Action> command in activeCommands)
                {
                    if (commandString == command.Key)
                    {
                        command.Value.Invoke();
                        goto START_LOOP;
                    }
                }

                // If no command was found
                Console.WriteLine($"Unrecognized command: {commandString}");
            }
        }
    }
}
