
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
using MineCosmos.Bot.Interactive;
using Sora.Entities;
using Sora.Entities.Segment;
using Newtonsoft.Json;
using System;
using Mapster;
using SqlSugar;
using MineCosmos.Bot.Service.Bot;
using MineCosmos.Bot.Service.Common;
using MineCosmos.Bot.Helper;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;

Console.WriteLine("[MineCosmos Bot Center]");


#region Host
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, configuration) =>
    {
        configuration.Sources.Clear();

        IHostEnvironment env = hostingContext.HostingEnvironment;
        configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

        IConfigurationRoot configurationRoot = configuration.Build();

        List<BotOptions> options = new();

        configurationRoot.GetSection("CqhttpQQ")
                         .Bind(options);

        BotOptions.Init(options);

        SqlSugarHelper.Instance.CodeFirst.InitTables(
       typeof(PlayerInfoEntity),
        typeof(PlayerSingInRecordEntity),
        typeof(TaskQueueEntity),
        typeof(MinecraftServerEntity),
        typeof(TimeEventHandleEntity)
        );

    })
    .ConfigureServices(services =>
    {
        services.TryAddSingleton<ICommonService, CommonService>();
    })
    .Build();

var commonService = host.Services.GetService<ICommonService>();
if (commonService is null)
{
    Console.WriteLine("MineCosmos Bot Center", "服务未能正常启动");
    Console.ReadKey();
    return;
}




#endregion

#region SelfSocket

_ = Task.Run(() =>
{
    //// 创建一个 IP 地址对象，表示要监听的主机和端口
    //IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    //int port = 7415;
    //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

    //// 创建一个 Socket 对象
    //Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    //// 将 Socket 绑定到要监听的地址和端口
    //listener.Bind(localEndPoint);

    //// 开始监听传入的连接请求
    //listener.Listen(10);

    //Console.WriteLine("Waiting for a connection...");

    //while (true)
    //{
    //    // 接受传入的连接请求，并创建一个新的 Socket 对象来处理连接
    //    Socket handler = listener.Accept();

    //    // 处理连接的逻辑
    //    Console.WriteLine($"Client connected from {handler.RemoteEndPoint}");

    //    byte[] buffer = new byte[1024];
    //    int bytesReceived = handler.Receive(buffer);
    //    string data = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
    //    Console.WriteLine($"Received data: {data}");

    //    // 发送回复消息到客户端
    //    string replyMessage = "Server received your message: " + data;
    //    byte[] replyBuffer = Encoding.ASCII.GetBytes(replyMessage);
    //    handler.Send(replyBuffer);

    //    // 关闭连接
    //    handler.Shutdown(SocketShutdown.Both);
    //    handler.Close();
    //}
});




#endregion


#region Sora

var service = SoraServiceFactory.CreateService(new ClientConfig() { Port = 8081 });

//启动服务并捕捉错误
_ = service.StartService()
             .RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));

service.Event.OnPrivateMessage += async (sender, eventArgs) =>
{
    string[] values = CommonFunction.GetCommand(eventArgs.Message.GetText());
    string command = values[0];
    var privateId = eventArgs.Sender.Id;
    switch (command)
    {
        case "test":
            var stream = commonService.GenerateImageToStream($"测试图片发送");
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
            //await serverManagerService.AppendServer(data);
            await eventArgs.Reply(new MessageBody { "注册成功\n", JsonConvert.SerializeObject(data) });
            break;
    }
};

service.Event.OnGroupMessage += async (sender, eventArgs) =>
{
    return;
    //屏蔽除ABC群外的所有群消息
    if (eventArgs.SourceGroup.Id != 588504056)
        return;

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


        return;

        #endregion

    }
    catch (Exception ex)
    {
        Console.WriteLine("群消息处理发生异常" + ex.Message);
    }

};

#endregion


#region KooK
await ServiceCentern.StartKookNet();
#endregion

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