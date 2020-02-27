using System;

namespace GameLocal
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main()
        {
            using (var game = new Networking_Game.Local.GameLocal())
                game.Run();
        }
    }
#endif
}
