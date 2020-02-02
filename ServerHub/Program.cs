using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking_Game;
using Networking_Game.ClientServer;

namespace ServerHub
{
    // TODO: create server hub
    static class Program
    {
        static List<Process> servers = new List<Process>();

        enum Command
        {
            EXIT,
            START,
            STOP,
            SERVER
        }

        static void Main(string[] args)
        {


            bool running = true;
            while (running)
            {
                // Wait for space bar to be pressed To input commands
                //Console.WriteLine("Press Space to input commands");
                //ConsoleKeyInfo readKey;
                //do readKey = Console.ReadKey(false);
                //while (readKey.KeyChar != ' ');

                string[] input = ConsoleManager.WaitGetPriorityInput("Input command: ").Split(' ');

                int i = 0;
                try
                {

                    if (Enum.TryParse(input[i++], true, out Command com))
                        switch (com)
                        {
                            case Command.EXIT:
                                running = false;
                                break;

                            case Command.START:
                                GameServerArguments StartServerArg = GameServerArguments.CreateFromConsoleInput();
                                Process newServer = Process.Start(Process.GetCurrentProcess().MainModule.FileName, StartServerArg.ToString());
                                servers.Add(newServer);
                                Console.WriteLine($"Started New server with ID:{newServer.Id}");
                                break;

                            case Command.STOP:
                                try
                                {
                                    int instanceId = int.Parse(input[i++]);
                                    Process instance = servers.Find(f => f.Id == instanceId);
                                    instance.Close(); // TODO: is this correct? use .Kill() instead?
                                    if (servers.Remove(instance)) Console.WriteLine($"Server {instanceId} stopped");
                                    else Console.WriteLine("failed to remove server from instances");
                                }
                                catch (ArgumentNullException)
                                {
                                    Console.WriteLine("ERROR: Instance not found.");
                                }

                                break;

                            case Command.SERVER:
                                //try
                                //{
                                //    int instanceID = int.Parse(input[i++]);
                                //    ServerInstance instance = servers.Find(f => f.ID == instanceID);

                                //}
                                //catch (Exception e)
                                //{
                                //    Console.WriteLine(e);
                                //    throw;
                                //}
                                break;

                            default:
                                Console.WriteLine($"Unrecognized command: {input[0]}");
                                break;
                        }
                    else
                    {
                        Console.WriteLine("Commands");
                        // List commands 
                        foreach (string name in Enum.GetNames(typeof(Command)))
                        {
                            Console.WriteLine(name);
                        }
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("syntax error");
                }
            }

            Console.WriteLine("Exited, press any key to continue");
            Console.ReadKey();
        }

        /// <summary>
        /// Hooks onto ProcessExit event Kill all servers on application exit
        /// </summary>
        private static void KillServersOnExit(Process[] servers)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                foreach (Process process in servers)
                {
                    process.Kill(); // TODO
                }
            };

        }
    }
}

