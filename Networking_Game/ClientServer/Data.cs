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
        FailedClaimSquare,
        EndGame,
        PlayerConnected,
        PlayerDisconnected,
        NextTurn,
        GridData
    }

    public static class PacketFactory
    {
        #region Write Extensions

        public static void Write(this NetOutgoingMessage message, PacketType paketType)
        {
            message.Write((ushort) paketType);
        }
        
        #endregion

        public static NetOutgoingMessage CreatePlayerConnectedMessage(this PlayerConnection client, NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.PlayerConnected);
            output.Write(ByteSerializer.ObjectToByteArray(client.player));
            return output;
        }

        public static NetOutgoingMessage CreatePlayerDisconnectedMessage(this PlayerConnection client, NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.PlayerDisconnected);
            output.Write(ByteSerializer.ObjectToByteArray(client.player));
            return output;
        }

        public static NetOutgoingMessage CreateNextTurnMessage(this PlayerConnection client, NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.NextTurn);
            output.Write(client.player.Name);
            return output;
        }

        public static NetOutgoingMessage CreateClaimSquareMessage(this Player player, NetPeer sender, int x, int y)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.ClaimSquare);
            output.Write(player.Name);
            output.Write(x);
            output.Write(y);
            return output;
        }

        public static NetOutgoingMessage CreateClaimSquareMessage(this NetClient client, int x, int y)
        {
            NetOutgoingMessage output = client.CreateMessage();
            output.Write(PacketType.ClaimSquare);
            output.Write(x);
            output.Write(y);
            return output;
        }

        public static NetOutgoingMessage CreateFailedClaimSquareMessage(NetPeer sender, string reason)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.FailedClaimSquare);
            output.Write(reason);
            return output;
        }


        public static NetOutgoingMessage CreateGridDataMessage(this Grid grid, NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.GridData);
            output.Write(ByteSerializer.ObjectToByteArray(grid));
            return output;
        }
        
        public static NetOutgoingMessage CreateEndGameMessage(NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.EndGame);
            //output.Write(ByteSerializer.ObjectToByteArray(players));
            return output;
        }
    }
}
