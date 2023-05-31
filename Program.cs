
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
        DbContext.Init(configurationRoot, hostingContext);

        DbContext.Db.CodeFirst.InitTables(
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

service.Event.OnPrivateMessage += async (sender, eventArgs) =>
{
    string[] values = CommonFunction.GetCommand(eventArgs.Message.GetText());
    string command = values[0];
    var privateId = eventArgs.Sender.Id;

    switch (command)
    {
        case "注册服务器":

            var msg = new MessageBody { "正确格式为：！注册服务器 <服务器名称> <服务器ip地址> <服务器端口号> <Rcon地址> <Rcon端口> <Rcon密码>" };

            if (values.Any(string.IsNullOrWhiteSpace) || values.Length < 7)
            {
                //context.QuickOperation.Reply?.Add("");                       
                await eventArgs.Reply( msg);
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
    var senderId = eventArgs.Sender.Id;
    var groupId = eventArgs.SourceGroup.Id;
    var card = eventArgs.SenderInfo.Card;
    string text = eventArgs.Message.GetText();

    if (groupId != 588504056)
        return;

    #region 终端显示      

    AnsiConsole.Write($"[red]原始消息：[/]{eventArgs.Message.RawText}");

    #endregion

    #region 避免风控
    await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 10)));
    #endregion

    var playerInfo = await DbContext.Db.Queryable<PlayerInfoEntity>().FirstAsync(a => a.QQ == senderId);

    #region 发言记录

    if (playerInfo == null)
    {
        var avator = await GroupFunction.DownloadQQImage(senderId.ToString());
        playerInfo = await DbContext.Db.Insertable(new PlayerInfoEntity
        {
            Avatar = avator,
            QQ = senderId,
            EmeraldVal = 100,
            Name = card,
        }).ExecuteReturnEntityAsync();
        var msg = new MessageBody();
        msg.Add(SoraSegment.At(senderId));
        msg.Add("\n第一次说话喔，送你绿宝石X100");
        await eventArgs.Reply(msg);

        await Task.Delay(500);
        //await actionSession.SendGroupMessageAsync(context.GroupId, new CqMessage { tts });
        //await actionSession.SendPrivateMessageAsync(context.GroupId, new CqMessage { tts });
        //await actionSession.SendPrivateMessageAsync(context.Sender.UserId, msg);
    }
    else
    {
        playerInfo.EmeraldVal += 1;
        playerInfo.UpdateUserId = playerInfo.Id;
        await DbContext.Db.Updateable(playerInfo).ExecuteCommandAsync();
    }

    #endregion

    #region 签到

    //签到
    await Task.Delay(300);
    var signInInfo = await DbContext.Db.Queryable<PlayerSingInRecordEntity>()
       .Where(a => a.PlayerId == playerInfo.Id && a.CreateTime == DateTime.Now.ToString("yyyy-MM-dd"))
     .OrderByDescending(a => a.CreateTime)
     .FirstAsync();
    bool isSignIn = signInInfo != null;
    if (!isSignIn)
    {
        var item = GroupFunction.GetItem();
        var integral = new Random().Next(1, 3);
        var emeraldVal = new Random().Next(5, 10);
        var luckColors = new List<string> { "红色", "黄色", "绿色", "蓝色", "紫色" };
        var luckColorndex = new Random().Next(0, luckColors.Count - 1);
        var luckNumber = new Random().Next(111, 999);
        var luckVal = new Random().Next(1, 100);

        var recordNum = await DbContext.Db.Queryable<PlayerSingInRecordEntity>()
           .Where(a => a.Id == playerInfo.Id)
           .CountAsync();

        await DbContext.Db.Insertable(new PlayerSingInRecordEntity
        {
            PlayerId = playerInfo.Id,
            CreateUserId = playerInfo.Id,
            UpdateUserId = playerInfo.Id,
            Integral = integral,
            EmeraldVal = emeraldVal,
            LuckColor = luckColors[luckColorndex],
            LuckNumber = luckNumber,
            LuckVal = luckVal,
        }).ExecuteCommandAsync();

        playerInfo.SignInCount += 1;
        playerInfo.EmeraldVal += emeraldVal;
        playerInfo.UpdateUserId = playerInfo.Id;
        await DbContext.Db.Updateable(playerInfo).ExecuteCommandAsync();

        var path = ImageGenerator.GenerateImage($"签到成功,第{recordNum}次签到");
        var msg = new MessageBody() { SoraSegment.At(senderId), SoraSegment.Image(path) };
        await eventArgs.Reply(msg);
    }

    #endregion

    #region 任务队列
    bool isTask = false;
    if (text.StartsWith("TTS:", StringComparison.OrdinalIgnoreCase)
        || text.Equals("我要挖鸭挖")
        || text.StartsWith("!UUID", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("！UUID", StringComparison.OrdinalIgnoreCase)
        )
    {
        isTask = true;
    }
    if (isTask)
    {
        AnsiConsole.Write($"[green]添加到任务队列：[/]{eventArgs.Message.RawText}");
        await GroupFunction.JoinTask(new()
        {
            ReviceMsg = eventArgs.Message.GetText(),
            SenderGroupId = groupId.ToString(),
            SenderId = senderId.ToString(),
            ReplyId = senderId.ToString(),
            ReplyGroupId = groupId.ToString(),
            GroupOrPrivate = 0,
            Type = 0,
            ExcuteTime = DateTime.Now.AddSeconds(3),
            IsExcute = 0,
        });
    }
    #endregion


};


//启动服务并捕捉错误
await service.StartService()
             .RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));

var api = service.GetApi(service.ServiceId);

await api.SendPrivateMessage(1714227099, "你吃饭了？");


//BotOptions.Get("WebSocketAddress", "QQ")
//CqWsSession cqWsSession = GetSession();
MyEventHandle.EnableEventHandle(api);



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