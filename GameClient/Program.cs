using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking_Game;


namespace GameClient
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new Networking_Game.ClientServer.GameClient())
            {
                game.Run();
            }
        }
    }
}
