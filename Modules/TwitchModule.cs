using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Modules
{
    [Group("twitch", "Twitch-related commands.")]
    public class TwitchModule : InteractionModuleBase<SocketInteractionContext>
    {

        public static TwitchService twitchService;

        public TwitchModule(DiscordSocketClient client)
        {
            if(twitchService == null)
            {
                twitchService = new TwitchService(client);
            }
        }

        [SlashCommand("info", "Print information on twitch info for this server.")]
        public async Task Twitch()
        {
            var guild = twitchService.GetGuild(Context.Guild.Id);
            if (guild != null)
            {
                var channel = Context.Guild.Channels.FirstOrDefault(x => x.Id == guild.textChannelID);
                if (channel != null)
                {
                    await RespondAsync($"Alerting for {guild.users.Count} users in "
                        + $"{channel.Name}.");
                    return;
                }
            }
            await RespondAsync("Module for sending alerts when channels go live.");
        }

        [SlashCommand("add-user", "Adds a user to send a message when they go live.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task AddUser(string username)
        {
            await RespondAsync(await twitchService.AddUser(Context.Guild.Id, Context.Channel.Id, username));
        }

        [SlashCommand("remove-user", "Remove the user from live reporting.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task RemoveUser(string username)
        {
            await RespondAsync(twitchService.RemoveUser(Context.Guild.Id, username));
        }

        [SlashCommand("users", "Prints the usernames of all users we report on.")]
        public async Task PrintUsers()
        {
            string[] users = twitchService.GetUsersInGuild(Context.Guild.Id);

            await RespondAsync($"Current Users: {String.Join(", ", users).Replace("_", "\\_")}");
        }

        [SlashCommand("set-text-channel", "Set the text channel that the alerts happen in.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task SetChannel(ISocketMessageChannel channel)
        {
            if (channel == null)
            {
                await RespondAsync("Channel is invalid.", ephemeral: true);
                return;
            }

            twitchService.SetAlertChannel(Context.Guild.Id, channel.Id);

            await RespondAsync($"Alerts are set to happen in {channel.Name}.");
        }

        [SlashCommand("display", "Change display mode.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task DisplayMode([Choice("No Image", 0), Choice("Stream Image", 1), Choice("Box Art", 2)]int mode)
        {
            bool result = twitchService.SetPreviewMode(Context.Guild.Id, mode);

            if (!result)
            {
                await RespondAsync($"Please add a channel to the report list first.");
                return;
            }

            await RespondAsync("Changed display mode.");
        }

        [SlashCommand("user-display", "Change display mode for a specific user.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task DisplayMode(string username, [Choice("No Image", 0), Choice("Stream Image", 1), Choice("Box Art", 2)] int mode)
        {
            bool result = twitchService.SetUserPreviewMode(Context.Guild.Id, username, mode);

            if (!result)
            {
                await RespondAsync($"Please add a channel to the report list first.");
                return;
            }

            await RespondAsync("Changed user's display mode.");
        }
    }
}
