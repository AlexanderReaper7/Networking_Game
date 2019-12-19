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

    public enum PacketType : byte
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

    public static class ByteSerializer
    {
        /// <summary>
        /// Converts an object to a byte array
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts a byte array to an Object
        /// </summary>
        /// <param name="arrBytes"></param>
        /// <returns></returns>
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
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
