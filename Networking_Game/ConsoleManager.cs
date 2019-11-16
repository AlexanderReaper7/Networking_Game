using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using CommandDotNet.MicrosoftCommandLineUtils;
using Console = Colorful.Console;

namespace Networking_Game
{
    public class ConsoleManager
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

        public ConsoleManager()
        {
            AllocConsole();
        }

        /// <summary>
        /// Sets the console window location and size in pixels
        /// </summary>
        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public void Initialize()
        {
            // Set focus to console
            Console.WriteLine("Setting Console to active window: " + (SetForegroundWindow(Handle) ? "Success" : "Failed"), Color.Gray);
            // Move console to top left
            Console.WriteLine("Moving console", Color.Gray);
            SetWindowPosition(-7, -7, 800, Program.Game.GraphicsDevice.Adapter.CurrentDisplayMode.Height);

        }

        public static bool IsPriorityInput = false;
        private static string priorityInput = null;
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
        public void Run()
        {
            // Console loop
            START_LOOP:
            while (true)
            {
                // Get input
                string input = Console.ReadLine();

                if (IsPriorityInput)
                {
                    priorityInput = input;
                    IsPriorityInput = false;
                    goto START_LOOP;
                }


                // Check Active commands
                foreach (KeyValuePair<string, Action> keyValuePair in activeCommands)
                {
                    if (input.StartsWith(keyValuePair.Key))
                    {
                        keyValuePair.Value.Invoke();
                        goto START_LOOP;
                    }
                }

                // Check standard commands
                switch (input)
                {
                    case "exit":
                    {
                        Program.Game.Exit();
                        return;
                    }
                    // If no command was found
                    default:
                    {
                        Console.WriteLine($"Unrecognized command: {input}");
                        break;
                    }
                }
            }
        }
    }


}
