using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineCosmos.Bot
{
    public class BotOptions
    {
        private static List<BotOptions> Bots = new List<BotOptions>();

        public string Type { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public object Val { get; set; }


        public static void Init(List<BotOptions> bots) { Bots = bots; }

        public static object Get(string key,string type)
        {
            return Bots.FirstOrDefault(predicate: a=>a.Key== key && a.Type==type)?.Val??throw new Exception($"{key} 配置项不存在");
        }

    }
}
