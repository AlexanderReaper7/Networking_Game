using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Tools_XNA_dotNET_Framework;

namespace Networking_Game.ClientServer
{

    public enum PacketType : ushort
    {
        ClaimSquare,
        EndGame,
        PlayerConnected,
        PlayerDisconnected
    }

    //public interface ICommand
    //{
    //    void Run(GameServer server, NetIncomingMessage message);
    //}

    //public class LoginCommand : ICommand
    //{
    //    public readonly string Name;
    //    public readonly PlayerShape Shape;
    //    public readonly Color Color;


    //    public void Run(GameServer server, NetIncomingMessage message)
    //    {
            
    //        // Parse data
    //        message.WriteAllFields(typeof(Player));
    //    }
    //}

    //public class ClaimSquareCommand : ICommand
    //{
    //    public void Run(GameServer server, NetIncomingMessage message)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class EndGameCommand : ICommand
    //{
    //    public void Run(GameServer server, NetIncomingMessage message)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    public static class PacketFactory
    {

        public static void Write(this NetOutgoingMessage message, PacketType paketType)
        {
            message.Write((ushort) paketType);
        }

        public static NetOutgoingMessage CreatePlayerConnectedMessage(this PlayerConnection connection)
        {
            NetOutgoingMessage output = connection.peer.CreateMessage();
            output.Write(PacketType.PlayerConnected);
            output.Write(ByteSerializer.ObjectToByteArray(connection.player));
            return output;
        }

        public static NetOutgoingMessage CreatePlayerDisconnectedMessage(this PlayerConnection connection)
        {
            NetOutgoingMessage output = connection.peer.CreateMessage();
            output.Write(PacketType.PlayerDisconnected);
            output.Write(ByteSerializer.ObjectToByteArray(connection.player));
            return output;
        }

    }
}
