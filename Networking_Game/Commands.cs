using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking_Game
{
    public static class Commands
    {
        public delegate void Code();

        public interface ICommand
        {
            string Key { get; }
            Code Code { get; }
        }

        public static Dictionary<string, Code> Dictionary = new Dictionary<string, Code>();

        public static void AddCommand(this ICommand command)
        {
            Dictionary.Add(command.Key, command.Code);
        }
    }
}
