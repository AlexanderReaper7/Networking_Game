using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Networking_Game.ClientServer
{

    public enum PacketType : UInt16
    {
        Login,
        ClaimSquare,
        EndGame,
    }

    public interface ICommand
    {
        void Run(GameServer server, NetIncomingMessage message);
    }

    public class LoginCommand : ICommand
    {
        public readonly string Name;
        public readonly PlayerShape Shape;
        public readonly Color Color;


        public void Run(GameServer server, NetIncomingMessage message)
        {
            
            // Parse data
            message.WriteAllFields(typeof(Player));
        }
    }

    public class ClaimSquareCommand : ICommand
    {
        public void Run(GameServer server, NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }
    }

    public class EndGameCommand : ICommand
    {
        public void Run(GameServer server, NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }
    }


    public static class PacketFactory
    {
        public static ICommand GetCommand(this PacketType packetType)
        {
            switch (packetType)
            {
                case PacketType.Login:
                case PacketType.ClaimSquare:
                case PacketType.EndGame: return new EndGameCommand();

                default:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null);
            }
        }
    }
}
