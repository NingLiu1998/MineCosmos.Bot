using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreRCON;
using MineCosmos.Bot.Entity;

namespace MineCosmos.Bot.Helper.Minecraft
{
    public class McServerManager
    {
        private static McServerManager _instance;
        public static McServerManager Instance => _instance ??= new McServerManager();

        private readonly List<MinecraftServerEntity> servers = new List<MinecraftServerEntity>();

        public void Add(MinecraftServerEntity model) => servers.Add(model);

        private async Task<MinecraftServerEntity> Get(string serverName) => servers.FirstOrDefault(a => a.ServerName == serverName);


        public async Task<string> SendCommandAsync(string serverName, string command)
        {
            var server = await Get(serverName);
            if (server == null) { Console.WriteLine($"{serverName}服务器不存在"); return "找不到服务器"; }
            ushort.TryParse(server.RconPort, out ushort port);
            var rconClient = new RCON(IPAddress.Parse(server.RconAddress), port, server.RconPwd);
            rconClient.OnDisconnected += (() => { Console.WriteLine("[RCON] 已断开连接"); });
            await rconClient.ConnectAsync();
            var response = await rconClient.SendCommandAsync(command);
            return response;
        }

        public async Task<List<string>> GetPlayerCountAsync(string serverName)
        {
            var server = await Get(serverName);
            ushort.TryParse(serverName, out ushort port);
            var rconClient = new RCON(IPAddress.Parse(server.RconAddress), port, server.RconPort);

            var playerList = await rconClient.SendCommandAsync("list");
            var players = MinecraftListParser.ParsePlayerList(playerList);
            foreach (var player in players)
            {
                Console.WriteLine(player);
            }
            return players;
        }

        public class MinecraftListParser
        {
            private static readonly Regex playerRegex = new Regex(@"(?<=\s)[a-zA-Z0-9_]+(?=\s)");

            public static List<string> ParsePlayerList(string playerList)
            {
                var matches = playerRegex.Matches(playerList);
                var players = new List<string>();
                foreach (Match match in matches)
                {
                    players.Add(match.Value);
                }
                return players;
            }
        }


        public async Task<List<string>> SendCommandToAllAsync(string command)
        {
            var responses = new List<string>();

            foreach (var server in servers)
            {
                ushort.TryParse(server.RconPort, out ushort port);
                var rconClient = new RCON(IPAddress.Parse(server.RconAddress), port, server.RconPort);
                await rconClient.ConnectAsync();
                var response = await rconClient.SendCommandAsync(command);
                rconClient.Dispose();
                responses.Add(response);
            }
            return responses;
        }
    }
}
