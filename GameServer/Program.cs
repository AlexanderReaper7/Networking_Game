using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Networking_Game;
using Networking_Game.ClientServer;

namespace GameServer
{
    public static class Program
    {
        private static List<ServerInstance> servers;
        public static bool running;

        [MTAThread] //TODO: Should this be STA or MTA?
        private static void Main(string[] args)
        {
            servers = new List<ServerInstance>();

            running = true;
            while (running)
            {
                //Console.WriteLine("Press Space to input commands");
                // Wait for space bar to be pressed
                //ConsoleKeyInfo readKey;
                //do readKey = Console.ReadKey(false);
                //while (readKey.KeyChar != ' ');

                string[] input = Console.ReadLine().Split(' ');

                int i = 0;
                try
                {

                if (Enum.TryParse(input[i++],true, out Command com))
                    switch (com)
                    {
                        case Command.EXIT:
                            running = false;
                            break;

                        case Command.START:
                            if (int.TryParse(input[i++], out int gameType))
                                switch ((GameType)gameType)
                                {
                                    case GameType.FillBoard:
                                        if (!int.TryParse(input[i++], out int x)) break;
                                        if (!int.TryParse(input[i++], out int y)) break;
                                        ServerInstance newServer = new ServerInstance((GameType) gameType, x, y);
                                        servers.Add(newServer);
                                        Console.WriteLine($"Started New server with ID:{newServer.ID}");
                                        break;
                                    default:
                                        Console.WriteLine("Usage: start (gametype) (grid size x) (grid size y)");
                                        break;
                                }
                            else Console.WriteLine("Usage: start (grid size x) (grid size y)");
                            break;

                        case Command.STOP:
                            try
                            {
                                int instanceID = int.Parse(input[i++]);
                                ServerInstance instance = servers.Find(f => f.ID == instanceID);
                                instance.Stop();
                                if (servers.Remove(instance)) Console.WriteLine($"Server {instanceID} stopped");
                                else Console.WriteLine("failed to remove server from instances");
                            }
                            catch (ArgumentNullException)
                            {
                                Console.WriteLine("ERROR: Instance not found.");
                            }
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
        }
    }

    public class ServerInstance
    {
        public readonly short ID;
        public Thread thread;
        public Networking_Game.ClientServer.GameServer server;

        public ServerInstance(GameType gameType, int gridSizeX, int gridSizeY)
        {
            ID = (short) new Random().Next(short.MaxValue);
            Networking_Game.ClientServer.GameServer server;
            thread = new Thread(() =>
                {
                    server = new Networking_Game.ClientServer.GameServer(gameType, gridSizeX, gridSizeY, Networking_Game.Program.DefaultPort);
                })
                {Name = $"Server_{ID}"};

            thread.Start();
        }

        public void Stop()
        {
            // TODO:Tell GameServer to stop
            thread.Abort();
        }
    }

    enum Command
    {
        EXIT,
        START,
        STOP,
    }
}

