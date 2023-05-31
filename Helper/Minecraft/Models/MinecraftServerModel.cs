using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineCosmos.Bot.Helper.Minecraft.Models
{
    public class MinecraftServerModel
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public ushort Port { get; set; }
        public string Password { get; set; }
    }
}
