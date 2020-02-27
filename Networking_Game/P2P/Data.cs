using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Tools_XNA_dotNET_Framework;
using Networking_Game;

namespace Networking_Game.P2P
{
    public enum PacketType : ushort
    {
        ClaimSquare,
        FailedClaimSquare,
        Ready,
        PlayerConnected,
        PlayerDisconnected,
        NextTurn,
        GridData,
        PlayerAssignment
    }

    public static class PacketFactory
    {
        #region Write Extensions

        public static void Write(this NetOutgoingMessage message, PacketType paketType)
        {
            message.Write((ushort)paketType);
        }

        #endregion

        public static NetOutgoingMessage CreatePlayerAssignmentMessage(NetPeer sender, List<PlayerConnection> connections, Player localPlayer, Grid grid)
        {
            List<PlayerConnection> pc = new List<PlayerConnection>(connections.Count+1);
            pc.Add(new PlayerConnection(localPlayer, GamePeer.HostIdentIP)); // TODO: find other way of ident host at non-hosts
            pc.AddRange(connections);

            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.PlayerAssignment);
            var cb = ByteSerializer.ObjectToByteArray(pc);
            var gb = ByteSerializer.ObjectToByteArray(grid);
            output.Write(cb.Length);
            output.Write(cb);
            output.Write(gb);
            return output;
        }

        public static NetOutgoingMessage CreatePlayerConnectedMessage(NetPeer sender, PlayerConnection player)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.PlayerConnected);
            output.Write(ByteSerializer.ObjectToByteArray(player));
            return output;
        }

        public static NetOutgoingMessage CreatePlayerDisconnectedMessage(NetPeer sender, PlayerConnection player)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.PlayerDisconnected);
            output.Write(ByteSerializer.ObjectToByteArray(player));
            return output;
        }

        public static NetOutgoingMessage CreateNextTurnMessage(NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.NextTurn);
            return output;
        }

        public static NetOutgoingMessage CreateClaimSquareMessage(NetPeer sender, Player player, int x, int y)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.ClaimSquare);
            output.Write(player.Name);
            output.Write(x);
            output.Write(y);
            return output;
        }

        //public static NetOutgoingMessage CreateClaimSquareMessage(NetPeer sender, int x, int y)
        //{
        //    NetOutgoingMessage output = sender.CreateMessage();
        //    output.Write(PacketType.ClaimSquare);
        //    output.Write(x);
        //    output.Write(y);
        //    return output;
        //}

        public static NetOutgoingMessage CreateFailedClaimSquareMessage(NetPeer sender, string reason)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.FailedClaimSquare);
            output.Write(reason);
            return output;
        }


        public static NetOutgoingMessage CreateGridDataMessage(NetPeer sender, Grid grid)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.GridData);
            output.Write(ByteSerializer.ObjectToByteArray(grid));
            return output;
        }

        public static NetOutgoingMessage CreateFoundGameMessage(NetPeer sender, FoundGame foundGame)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(ByteSerializer.ObjectToByteArray(foundGame));
            return output;
        }

        public static NetOutgoingMessage CreateStartGameMessage(NetPeer sender)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(PacketType.Ready);
            return output;
        }

        public static NetOutgoingMessage CreateConnectionApprovalMessage(NetPeer sender, IPEndPoint[] otherPeers)
        {
            NetOutgoingMessage output = sender.CreateMessage();
            output.Write(otherPeers.Length);
            for (int i = 0; i < otherPeers.Length; i++)
            {
                output.Write(otherPeers[i]);
            }
            return output;
        }
    }

    [Serializable]
    public class PlayerConnection
    {
        public Player player;
        public IPEndPoint ipEndPoint;
        public bool isReady;


        public PlayerConnection(Player player, IPEndPoint ipEndPoint)
        {
            this.player = player;
            this.ipEndPoint = ipEndPoint;
            isReady = true;
        }
    }
}
