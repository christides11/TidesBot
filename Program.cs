using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Modules;
using TidesBotDotNet.Services;
using Victoria;

namespace TidesBotDotNet
{
    public class Program
    {
        private DiscordSocketClient client;
        private CommandService commandService;
        private CommandHandler commandHandler;
        private LavaConfig lavaConfig;
        private LavaNode lavaNode;
        private BotDefinition botDefinition;
        private GuildsDefinition guildsDefinition;

        private ReactionRoleService reactionRoleService;

        private string token;

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton(botDefinition)
            .AddSingleton(client)
            .AddSingleton(commandService)
            .AddSingleton(lavaConfig)
            .AddSingleton(lavaNode)
            .AddSingleton(guildsDefinition)
            .AddSingleton(reactionRoleService)
            .AddSingleton<DJService>()
            .BuildServiceProvider();

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            if (!LoadBotDefinition())
            {
                throw new Exception("No token provided.");
            }

            guildsDefinition = new GuildsDefinition();

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100
            });

            client.Log += Log;
            client.Ready += OnReadyAsync;

            lavaConfig = new LavaConfig();
            lavaNode = new LavaNode(client, lavaConfig);

            reactionRoleService = new ReactionRoleService(client);

            commandService = new CommandService();
            commandHandler = new CommandHandler(BuildServiceProvider(), client, commandService, botDefinition);

            await commandHandler.InstallCommandsAsync();

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            await client.SetGameAsync(botDefinition.GetCurrent().status, null, ActivityType.Playing);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private bool LoadBotDefinition()
        {
            string bd = SaveLoadService.Load("botdefinition.json");
            if (bd != null)
            {
                botDefinition = JsonConvert.DeserializeObject<BotDefinition>(bd);
                try
                {
                    token = botDefinition.GetCurrent().token;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private async Task OnReadyAsync()
        {
            await lavaNode.ConnectAsync();
        }
    }
}
