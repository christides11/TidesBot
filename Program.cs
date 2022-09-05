using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace TidesBotDotNet
{
    public class Program
    {
        private BotDefinition botDefinition;
        private GuildsDefinition guildsDefinition;

        private string token;

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
                    & ~(GatewayIntents.GuildPresences
                    | GatewayIntents.GuildScheduledEvents
                    | GatewayIntents.GuildInvites),
                LogGatewayIntentWarnings = false,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 100
            }))
            .AddSingleton<NodeConfiguration>()
            .AddSingleton(x => new LavaNode<LavaPlayer, LavaTrack>(x.GetRequiredService<DiscordSocketClient>(), x.GetRequiredService<NodeConfiguration>(), null))
            .AddSingleton(botDefinition)
            .AddSingleton(guildsDefinition)
            .AddSingleton<InteractionService>()
            .AddSingleton<CommandHandler>()
            .AddSingleton<ReactionRoleService>()
            .AddSingleton<AutoRolesService>()
            .AddSingleton<DJService>()
            .BuildServiceProvider();

        private IServiceProvider provider;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();


        public async Task MainAsync()
        {
            guildsDefinition = new GuildsDefinition();

            if (!LoadBotDefinition())
            {
                throw new Exception("No token provided.");
            }

            using IServiceScope serviceScope = BuildServiceProvider().CreateScope();
            provider = serviceScope.ServiceProvider;

            var client = provider.GetRequiredService<DiscordSocketClient>();

            client.Log += Log;
            client.Ready += OnReadyAsync;

            await provider.GetRequiredService<CommandHandler>().InstallCommandsAsync();

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
            //await lavaNode.ConnectAsync();
            //lavaNode.OnTrackEnd += OnTrackEnded;

            await provider.GetRequiredService<InteractionService>().RegisterCommandsGloballyAsync(true);
        }

        private async Task OnTrackEnded(TrackEndEventArg<LavaPlayer, LavaTrack> arg)
        {
            Console.WriteLine("???");
        }
    }
}
