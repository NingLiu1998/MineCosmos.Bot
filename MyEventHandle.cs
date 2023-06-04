using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kook.Commands;
using MineCosmos.Bot.Entity;
using MineCosmos.Bot.Helper;
using MineCosmos.Bot.Helper.Minecraft;
using MineCosmos.Bot.Interactive;
using MineCosmos.Bot.Service;
using Newtonsoft.Json.Linq;
using Sora.Entities;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;

namespace MineCosmos.Bot;


/// <summary>
/// 事件处理
/// </summary>
internal class MyEventHandle
{


    public static void EnableEventHandle(SoraApi wsSession)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                var info = await SqlSugarHelper.Instance.Queryable<TaskQueueEntity>()
                 .Where(expression: a => a.IsExcute == 0 && a.ExcuteTime <= DateTime.Now)
                 .OrderByDescending(a => a.CreateTime)
                .FirstAsync();
                if (info == null) continue;
                if (string.IsNullOrWhiteSpace(info.ReviceMsg)) continue;
                string text = info.ReviceMsg;

                //解析
                string[] values = CommonFunction.GetCommand(text);
                string command = values[0];

                long.TryParse(info.SenderGroupId, out long groupId);
                long.TryParse(info.SenderId, out long senderId);
                MessageBody msg = "NotAbout";

                if (command.StartsWith("TTS:", StringComparison.OrdinalIgnoreCase))
                {
                    msg = TTSHandle(command);
                }

                //if (command.StartsWith("UUID", StringComparison.OrdinalIgnoreCase))
                //{
                //    msg = await UUIDHandle(values, info.SenderId);
                //}


                if (info.GroupOrPrivate.Equals(0))
                {
                    await wsSession.SendGroupMessage(groupId, msg);

                }
                else
                {
                    await wsSession.SendPrivateMessage(senderId, msg);
                }


                await SqlSugarHelper.Instance.Deleteable(info).ExecuteCommandAsync();
            }
        });
    }

    public static MessageBody TTSHandle(string command)
    {
       var s = new MessageBody();
        s.Add(SoraSegment.Record("测试", true, true));
       return s;

        //return new CqMessage { new CqTtsMsg(command[4..]) };
    }

    
}

