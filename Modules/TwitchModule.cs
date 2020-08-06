using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Services;
using TwitchLib.Api;


namespace TidesBotDotNet.Modules
{
    [Group("twitch")]
    public class TwitchModule : ModuleBase<SocketCommandContext>
    {

        public static TwitchService twitchService;

        private DiscordSocketClient client;

        public TwitchModule(DiscordSocketClient client)
        {
            if(twitchService == null)
            {
                twitchService = new TwitchService(client);
            }
            this.client = client;
        }

        [Command]
        public async Task Twitch()
        {
            var guild = twitchService.GetGuild(Context.Guild.Id);
            if (guild != null)
            {
                var channel = Context.Guild.Channels.FirstOrDefault(x => x.Id == guild.textChannelID);
                if (channel != null)
                {
                    await Context.Channel.SendMessageAsync($"Alerting for {guild.users.Count} users in "
                        + $"{channel.Name}.");
                    return;
                }
            }
            await Context.Channel.SendMessageAsync("Module for sending alerts when channels go live.");
        }

        [Command("adduser")]
        [Alias("add", "a")]
        [Summary("Adds a user to send a message when they go live.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task AddUser(params String[] usernames)
        {
            if(usernames.Count() > 1)
            {
                foreach(string username in usernames)
                {
                    await AddUser(username);
                }
                return;
            }

            var m = await Context.Channel.SendMessageAsync(await twitchService.AddUser(Context.Guild.Id, Context.Channel.Id, usernames[0]));
        }

        [Command("removeuser")]
        [Alias("remove", "r")]
        [Summary("Remove the user from live reporting.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task RemoveUser(params String[] usernames)
        {
            if (usernames.Count() > 1)
            {
                foreach (string username in usernames)
                {
                    await RemoveUser();
                }
                return;
            }

            var m = await Context.Channel.SendMessageAsync(twitchService.RemoveUser(Context.Guild.Id, usernames[0]));
        }

        [Command("users")]
        [Alias("u")]
        [Summary("Prints the usernames of all users we report on.")]
        public async Task PrintUsers()
        {
            string[] users = twitchService.GetUsersInGuild(Context.Guild.Id);

            await Context.Channel.SendMessageAsync($"Current Users: {String.Join(", ", users)}");
        }

        [Command("setchannel")]
        [Alias("channel", "c")]
        [Summary("Set the channel that the alert happens in.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task SetChannel(string param)
        {
            var channel = Context.Message.MentionedChannels.FirstOrDefault();

            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("No channel mentioned.");
                return;
            }

            twitchService.SetAlertChannel(Context.Guild.Id, channel.Id);

            await Context.Channel.SendMessageAsync($"Alerts are set to happen in {channel.Name}.");
        }

        [Command("display")]
        [Alias("d")]
        [Summary("0 = no image, 1 = stream image, 2 = boxart")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task DisplayMode(int mode)
        {
            bool result = twitchService.SetPreviewMode(Context.Guild.Id, mode);

            if (!result)
            {
                await Context.Channel.SendMessageAsync($"Please add a channel to the report list first.");
                return;
            }

            await Context.Channel.SendMessageAsync("Changed display mode.");
        }
    }
}
