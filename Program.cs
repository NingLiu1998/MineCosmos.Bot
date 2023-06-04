
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MineCosmos.Bot;
using MineCosmos.Bot.Entity;
using MineCosmos.Bot.Service;
using Sora.Net.Config;
using Sora;
using Spectre.Console;
using static System.Net.Mime.MediaTypeNames;
using Sora.Util;
using YukariToolBox.LightLog;
using Kook;
using MineCosmos.Bot.Helper;
using MineCosmos.Bot.Interactive;
using Sora.Entities;
using Sora.Entities.Segment;
using MineCosmos.Bot.Helper.Minecraft;
using Newtonsoft.Json;
using System;
using Mapster;
using SqlSugar;

Console.WriteLine("[MineCosmos Bot Center]");


#region Host
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, configuration) =>
    {
        configuration.Sources.Clear();

        IHostEnvironment env = hostingContext.HostingEnvironment;


        configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

        IConfigurationRoot configurationRoot = configuration.Build();

        List<BotOptions> options = new();

        configurationRoot.GetSection(nameof(BotOptions))
                         .Bind(options);

        BotOptions.Init(options);


        //SqlSugarHelper.InstanceConfigs = configurationRoot.GetSection("ConnectionStrings:SqlSugarConfig")
        //    .Get<List<SqlSugarConfig>>(); 

        SqlSugarHelper.Instance.CodeFirst.InitTables(
       typeof(PlayerInfoEntity),
        typeof(PlayerSingInRecordEntity),
        typeof(TaskQueueEntity),
        typeof(MinecraftServerEntity),
        typeof(TimeEventHandleEntity)
        );



        //Console.WriteLine($"TransientFaultHandlingOptions.Enabled={options.Enabled}");
        //Console.WriteLine($"TransientFaultHandlingOptions.AutoRetryDelay={options.AutoRetryDelay}");
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<ServiceLifetimeReporter>();
    })
    .Build();

#endregion


var service = SoraServiceFactory.CreateService(new ClientConfig() { Port = 8081 });

//启动服务并捕捉错误
await service.StartService()
             .RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));

#region 测试信息
var stream = ImageGenerator.GenerateImageToStream("第一行内容 123456 \r\n 第二行内容 \r\n 第三行内容");
await service.GetApi(service.ServiceId)
    .SendPrivateMessage(1714227099, new MessageBody
    { SoraSegment.Image(stream), SoraSegment.Text("下午好") });
#endregion

service.Event.OnPrivateMessage += async (sender, eventArgs) =>
{
    string[] values = CommonFunction.GetCommand(eventArgs.Message.GetText());
    string command = values[0];
    var privateId = eventArgs.Sender.Id;



    switch (command)
    {
        case "test":
            var stream = ImageGenerator.GenerateImageToStream($"测试图片发送");
            await eventArgs.Reply(new MessageBody { SoraSegment.Image(stream) });
            break;
        case "注册服务器":

            var msg = new MessageBody { "正确格式为：！注册服务器 <服务器名称> <服务器ip地址> <服务器端口号> <Rcon地址> <Rcon端口> <Rcon密码>" };

            if (values.Any(string.IsNullOrWhiteSpace) || values.Length < 7)
            {
                //context.QuickOperation.Reply?.Add("");                       
                await eventArgs.Reply(msg);
                return;
            }

            string serverName, serverIp, serverPort, serverRconIp, serverRconPort, serverRconPwd;
            serverName = values[1];
            serverIp = values[2];
            serverPort = values[3];
            serverRconIp = values[4];
            serverRconPort = values[5];
            serverRconPwd = values[6];
            MinecraftServerEntity data = new()
            {
                ServerName = serverName,
                ServerIp = serverIp,
                ServerPort = serverPort,
                RconAddress = serverRconIp,
                RconPort = serverRconPort,
                RconPwd = serverRconPwd
            };
            await MinecraftServerHelper.AppendServer(data);
            await eventArgs.Reply(new MessageBody { "注册成功\n", JsonConvert.SerializeObject(data) });
            break;
    }
};

service.Event.OnGroupMessage += async (sender, eventArgs) =>
{
    try
    {

        var senderId = eventArgs.Sender.Id;
        var groupId = eventArgs.SourceGroup.Id;
        var card = eventArgs.SenderInfo.Card;
        string text = eventArgs.Message.GetText();
        int msgId = eventArgs.Message.MessageId;

        if (groupId != 588504056)
            return;

        #region 终端显示      

        AnsiConsole.Write($"[red]原始消息：[/]{eventArgs.Message.RawText}");

        #endregion

        #region 避免风控
        await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 10)));
        #endregion

        var playerInfo = await SqlSugarHelper.Instance.CopyNew().Queryable<PlayerInfoEntity>().FirstAsync(a => a.QQ == senderId);

        #region 发言记录

        if (playerInfo == null)
        {
            var avator = await GroupFunction.DownloadQQImage(senderId.ToString());
            playerInfo = await SqlSugarHelper.Instance.Insertable(new PlayerInfoEntity
            {
                Avatar = avator,
                QQ = senderId,
                EmeraldVal = 100,
                Name = card,
            }).ExecuteReturnEntityAsync();

            var imgStream = ImageGenerator.GenerateImageToStream("第一次在Q群发言 o.O \r\n 送你100个绿宝石", 10);
            MessageBody msg = SoraSegment.Reply(msgId) + SoraSegment.Image(imgStream);
            await eventArgs.Reply(msg);
            //await Task.Delay(1200);
        }
        else
        {
            playerInfo.EmeraldVal += 1;
            playerInfo.UpdateUserId = playerInfo.Id;
            await SqlSugarHelper.Instance.Updateable(playerInfo).ExecuteCommandAsync();
        }

        #endregion

        #region 签到
        var signInInfo = await SqlSugarHelper.Instance.Queryable<PlayerSingInRecordEntity>()
           .Where(a => a.PlayerId == playerInfo.Id && a.CreateTime == DateTime.Now.ToString("yyyy-MM-dd"))
             .OrderByDescending(a => a.CreateTime)
         .FirstAsync();

        if (signInInfo == null)
        {
            var msg = await GroupFunction.SignlInAsyncMessage(msgId, playerInfo);
            await eventArgs.Reply(msg);
        }
        #endregion

        #region 功能



       //string command = msgText.Substring(0, Math.Max(position1, position2));

        //校验指令
        string msgText = eventArgs.Message.GetText();
        if (string.IsNullOrWhiteSpace(msgText)) return;
        int position = Math.Max(msgText.IndexOf("！"), msgText.IndexOf("!"));
        if (position < 0) return;
        //解析指令最终值
        string[] values = CommonFunction.GetCommand(text);
        string command = values[0];
        //定义即将发送的消息
        MessageBody messageBody = null;
        if (command.StartsWith("uuid", StringComparison.OrdinalIgnoreCase))
        {
            messageBody = await GroupFunction.QueryUUIDInfoMessageAsync(values, msgId);

        }
        else if (command.StartsWith("skin", StringComparison.OrdinalIgnoreCase))
        {
            messageBody = await GroupFunction.GetMinecraftPlayerSkin(values, msgId);
        }
        else if (command.StartsWith("server")|| command.StartsWith("服务器信息"))
        {
            messageBody = await GroupFunction.GetServerInfo(values, msgId);
        }else if (command.StartsWith("我的信息"))
        {
            messageBody = await GroupFunction.GetSystemInfo(values, msgId);
        }

        //统一使用回复消息
        if (messageBody != null)            
            await eventArgs.Reply(messageBody);

        return;

        #endregion

        #region 任务 TODO 
        //AnsiConsole.Write($"[green]添加到任务队列：[/]{eventArgs.Message.RawText}");
        //await GroupFunction.JoinTask(new()
        //{
        //    ReviceMsg = eventArgs.Message.GetText(),
        //    SenderGroupId = groupId.ToString(),
        //    SenderId = senderId.ToString(),
        //    ReplyId = senderId.ToString(),
        //    ReplyGroupId = groupId.ToString(),
        //    GroupOrPrivate = 0,
        //    Type = 0,
        //    ExcuteTime = DateTime.Now.AddSeconds(3),
        //    IsExcute = 0,
        //});
        #endregion

    }
    catch (Exception ex)
    {
        Console.WriteLine("群消息处理发生异常" + ex.Message);
    }



};


//BotOptions.Get("WebSocketAddress", "QQ")
//CqWsSession cqWsSession = GetSession();
MyEventHandle.EnableEventHandle(service.GetApi(service.ServiceId));



//定时执行队列任务











while (true)
{
    if (Console.ReadLine() == "exit")
    {
        break;
    }
    else
    {
        AnsiConsole.Write(new Panel(
        Align.Center(
            new Markup("输入 [red]exit[/] 退出Bot!"),
            VerticalAlignment.Middle)));
    }
}