using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;
using Victoria.Node;

namespace TidesBotDotNet
{
    public class Program
    {
        private DiscordSocketClient client;
        private CommandService commandService;
        private CommandHandler commandHandler;
        private NodeConfiguration lavaConfig;
        private LavaNode lavaNode;

        private BotDefinition botDefinition;
        private GuildsDefinition guildsDefinition;

        private ReactionRoleService reactionRoleService;

        private string token;

        private Microsoft.Extensions.Logging.Logger<LavaNode> lavaLogger;

        private AutoRolesService autoRoleService;

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton(botDefinition)
            .AddSingleton(client)
            .AddSingleton(commandService)
            .AddSingleton(lavaConfig)
            .AddSingleton(lavaNode)
            .AddSingleton(guildsDefinition)
            .AddSingleton(reactionRoleService)
            .AddSingleton(autoRoleService)
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

            //var cfg = new DiscordSocketConfig();
            //cfg.GatewayIntents |= GatewayIntents.GuildMembers;
            //cfg.GatewayIntents |= GatewayIntents.GuildMessageReactions;

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                GatewayIntents = GatewayIntents.All
            });

            client.Log += Log;
            client.Ready += OnReadyAsync;

            lavaConfig = new NodeConfiguration();
            lavaNode = new LavaNode(client, lavaConfig, lavaLogger);

            autoRoleService = new AutoRolesService(client);
            reactionRoleService = new ReactionRoleService(client, guildsDefinition);

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
            /*
            await lavaNode.ConnectAsync();

            ulong guildId = 283863914591027200;
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = client.GetGuild(guildId);

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            guildCommand.WithName("first-command");

            // Descriptions can have a max length of 100.
            guildCommand.WithDescription("This is my first guild slash command!");

            // Let's do our global command
            var globalCommand = new SlashCommandBuilder();
            globalCommand.WithName("first-global-command");
            globalCommand.WithDescription("This is my frist global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());

                // With global commands we dont need the guild.
                //await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            */
        }
    }
}
