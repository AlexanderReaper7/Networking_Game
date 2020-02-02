using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        [MTAThread] //TODO: Should this be STA or MTA?
        private static void Main(string[] args)
        {
            // Start console
            ConsoleManager.Start();
            // Wait for console
            Thread.Sleep(1000);
            // TODO: Parse args
            if (true)
            {
                // Start serverInstance
                if (true)
                {
                    ServerInstance serverInstance;

                    string input = ConsoleManager.WaitGetPriorityInput("Input port or leave blank to start on default port: ");
                    serverInstance = int.TryParse(input, out int port)
                        ? new ServerInstance(port)
                        : new ServerInstance();
                }
                else
                {
                    Console.WriteLine("Invalid startup argument");
                    Console.ReadKey();
                }
            }
        }
    }

    public class ServerInstance
    {
        public int ID => Process.GetCurrentProcess().Id;
        private Networking_Game.ClientServer.GameServer server;

        /// <summary>
        /// Creates and start a new server
        /// </summary>
        /// <param name="port"></param>
        public ServerInstance(int port = Networking_Game.Program.DefaultPort)
        { 
            server = new Networking_Game.ClientServer.GameServer(port);
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void Stop()
        {
            // Tell GameServer to stop
            server.StopServer();
            // TODO: remove from instances
        }
    }

}

