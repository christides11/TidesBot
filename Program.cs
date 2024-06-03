using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.DiscordNet;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;
using Lavalink4NET.Tracking;

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
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
                LogGatewayIntentWarnings = false,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 100
            }))
            .AddSingleton(botDefinition)
            .AddSingleton(guildsDefinition)
            .AddSingleton<IAudioService, LavalinkNode>()
            .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
            .AddSingleton(new LavalinkNodeOptions {
                RestUri = "http://localhost:2333/",
                WebSocketUri = "ws://localhost:2333/",
                Password = "youshallnotpass"
            })
            .AddSingleton(new InactivityTrackingOptions{
                DisconnectDelay = TimeSpan.FromSeconds(10),
                PollInterval = TimeSpan.FromSeconds(4),
                TrackInactivity = true
             })
            .AddSingleton<InactivityTrackingService>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<Fergun.Interactive.InteractiveService>()
            .AddSingleton<CommandHandler>()
            .AddSingleton<ReactionRoleService>()
            .AddSingleton<AutoRolesService>()
            .AddSingleton<VxTwitterService>()
            .AddSingleton<TwitchService>()
            .AddSingleton<StreamRoleService>()
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
            await provider.GetRequiredService<InteractionService>().RegisterCommandsGloballyAsync(true);
            await provider.GetRequiredService<IAudioService>().InitializeAsync();
        }
    }
}
