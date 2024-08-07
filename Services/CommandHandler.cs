using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using System.Reflection;
using System;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using Discord;

namespace TidesBotDotNet.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider services;
        private readonly BotDefinition botDefinition;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, InteractionService commands, BotDefinition botDefinition)
        {
            _commands = commands;
            _client = client;
            this.services = services;
            this.botDefinition = botDefinition;
        }

        public async Task InstallCommandsAsync()
        {
            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            // Hook the MessageReceived event into our command handler
            //_client.MessageReceived += HandleCommandAsync;
            _client.InteractionCreated += HandleInteraction;

            _commands.SlashCommandExecuted += SlashCommandExecuted;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(ctx, services);
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            return Task.CompletedTask;
        }

        /*
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix(botDefinition.GetCurrent().prefix.ToUpper(), ref argPos) ||
                message.HasStringPrefix(botDefinition.GetCurrent().prefix.ToLower(), ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) 
                || message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.

            // Keep in mind that result does not indicate a return value
            // rather an object stating if the command executed successfully.
            //var result = await _commands.ExecuteAsync(
            //    context: context,
            //    argPos: argPos,
            //    services: services);

            // Optionally, we may inform the user if the command fails
            // to be executed; however, this may not always be desired,
            // as it may clog up the request queue should a user spam a
            // command.
            // if (!result.IsSuccess)
            // await context.Channel.SendMessageAsync(result.ErrorReason);
        }*/
    }
}
