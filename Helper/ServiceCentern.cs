using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kook;
using Kook.Commands;
using Kook.Rest;
using Kook.WebSocket;
using MineCosmos.Bot.Service.Bot;
using MineCosmos.Bot.Service.Common;
using Spectre.Console;
using YukariToolBox.LightLog;

namespace MineCosmos.Bot.Helper
{
    internal static class ServiceCentern
    {

        public static ICommonService? commonService;


        public static KookSocketClient _client;


        public static async Task StartKookNet()
        {
            _client = new KookSocketClient();
            var token = "1/MTc1MDk=/RLqHR0uH/bGx/XppIvczXA==";
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            _client.Ready += () =>
            {
                AnsiConsole.Markup("[red]Kook 作战机甲已觉醒 [/] ");
                return Task.CompletedTask;
            };
            _client.MessageUpdated += MessageUpdated; ;
            _client.MessageReceived += MessageReceived;

            //


            StringBuilder sb = new();
            foreach (var item in _client.Guilds)
            {
                sb.AppendLine(item.Name);
            }

            AnsiConsole.Write(new Panel(
        Align.Center(
            new Markup($"[red]{sb.ToString()}[/]!"),
            VerticalAlignment.Middle)));


        }

        private static async Task MessageReceived(SocketMessage message, SocketGuildUser guildUser, SocketTextChannel textChannel)
        {


            //RestMessage.AddReactionAsync(message, new RequestOptions { });

            AnsiConsole.Write(new Rows(new List<Text>(){
            new Text($"{guildUser.Nickname}/{guildUser.DisplayName}",
          new Style(Spectre.Console.Color.Red, Spectre.Console.Color.Black)),

            new Text($"发言人：{guildUser.Nickname}/{guildUser.DisplayName} , 频道名称： {textChannel.Name}, 原始消息：{message.RawContent}",
            new Style(Spectre.Console.Color.Green, Spectre.Console.Color.Black)),

            new Text($"{message.RawContent}", new Style(Spectre.Console.Color.Blue, Spectre.Console.Color.Black))
            }));


            await message.AddReactionAsync(new Kook.Emoji("👌"));
        }

        private static async Task MessageUpdated(Cacheable<SocketMessage, Guid> arg1, Cacheable<SocketMessage, Guid> arg2, SocketTextChannel arg3)
        {
            AnsiConsole.Write(new Rows(new List<Text>(){
            new Text($"服务器ID：{arg1.Id}| 用户： {arg1.Value.Author.Username} | 消息：{arg1.Value.Content}  ---> {arg2.Value.Content} RawContent: {arg1.Value.RawContent}",
          new Style(Spectre.Console.Color.Red, Spectre.Console.Color.Black)),
            }));
            await arg1.Value.AddReactionAsync(new Kook.Emoji("👀"));
        }
    }
}
