using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreRCON;
using MineCosmos.Bot.Entity;
using MineCosmos.Bot.Service;

namespace MineCosmos.Bot.Helper.Minecraft
{
    internal class MinecraftServerHelper
    {
        /// <summary>
        /// 添加指令
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static async Task AppendCommandToServer(MinecraftServerCommandEntity model)
        {
            if (!await DbContext.Db.Queryable<MinecraftServerCommandEntity>().AnyAsync(a => a.Command == model.Command && a.ServerId == model.ServerId))
            {
                await DbContext.Db.Insertable(model).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 返回指令列表
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public static async Task<List<MinecraftServerCommandEntity>> GetLstCommand(int serverId)
        {
            return await DbContext.Db.Queryable<MinecraftServerCommandEntity>().Where(a => a.ServerId == serverId).ToListAsync();
        }


        public static async Task AppendServer(MinecraftServerEntity model)
        {
            if (!await DbContext.Db.Queryable<MinecraftServerEntity>().AnyAsync(a => a.ServerName == model.ServerName))
            {
                await DbContext.Db.Insertable(model).ExecuteCommandAsync();
                McServerManager.Instance.Add(model);
            }
        }

        public static async Task<string> SendAsync(string serverName, string command)
        {
            return await McServerManager.Instance.SendCommandAsync(serverName, command);
        }
    }


}
