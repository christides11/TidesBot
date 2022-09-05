using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;
using TwitchLib.Api;

namespace TidesBotDotNet.Modules
{
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        public GuildsDefinition guildsDefinition;

        public UserModule(GuildsDefinition gd)
        {
            guildsDefinition = gd;
        }

        [Command("colorme")]
        [Alias("cm")]
        [Summary("Gives the user a color based on the hexcode given.")]
        public async Task ColorMe(string color)
        {
            // Check if this guild supports ColorMe.
            if (!guildsDefinition.GetSettings(Context.Guild.Id).colorMe)
            {
                await Context.Channel.SendMessageAsync("ColorMe is not enabled for this server.");
                return;
            }

            string roleName = ("colorme" + Context.User.Id);
            // Check if the color is valid.
            Discord.Color roleColor = new Discord.Color();
            try
            {
                roleColor = new Discord.Color(GetRawColor(color));
            }
            catch
            {
                await Context.Channel.SendMessageAsync("Invalid hex code.");
                return;
            }
            // Check if the user already has a color role.
            IRole colorRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            // Get where this role should go so it works, which is right above their current role.
            int highestRole = Context.Guild.GetUser(Context.User.Id).Roles.LastOrDefault(x => x.Name != roleName).Position+1;

            // Create the role if needed.
            if (colorRole == null)
            {
                colorRole = await Context.Guild.CreateRoleAsync(roleName, null, roleColor, false);
                await colorRole.ModifyAsync(new Action<RoleProperties>(x => x.Position = highestRole));
            }
            else
            {
                await colorRole.ModifyAsync(new Action<RoleProperties>(x => x.Color = roleColor));
            }

            // If the user doesn't have the color role assigned, add it to them.
            if (Context.Guild.GetUser(Context.User.Id).Roles.FirstOrDefault(x => x.Name == roleName) == null)
            {
                try
                {
                    await Context.Guild.GetUser(Context.User.Id).AddRolesAsync(new List<IRole>() { colorRole });
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            await Context.Channel.SendMessageAsync($"Assigned color {roleColor.ToString()} to {Context.User.Username}.");
        }

        [Command("colormer")]
        [Alias("cmr")]
        [Summary("Removes the color role from the user.")]
        public async Task ColorMeRemove()
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "colorme" + Context.User.Id);
            if(role != null)
            {
                await role.DeleteAsync();
            }
            await Context.Channel.SendMessageAsync("Removed color role.");
        }

        [Command("avatar")]
        public async Task Avatar(params string[] users)
        {
            await Avatar(1024, users);
        }

        [Command("avatar")]
        [Summary("Gets the avatar of the user(s). Size must be a power of 2 between 16 and 2048." +
            "You do not have to @ the user(s), just type their username(s).")]
        public async Task Avatar(ushort size, params string[] users)
        {
            ImageFormat format = ImageFormat.Auto;
            foreach (SocketUser su in Context.Message.MentionedUsers)
            {
                await AvatarTask(su, format, size);
            }
            foreach(string user in users)
            {
                SocketGuildUser u = Context.Guild.Users.FirstOrDefault(x => x.Username.ToLower() == user.ToLower());
                if(u != null)
                {
                    await AvatarTask(u, format, size);
                }
            }
        }

        public async Task AvatarTask(SocketUser user, ImageFormat format, ushort size)
        {
            EmbedBuilder output = new EmbedBuilder();
            output.WithImageUrl(user.GetAvatarUrl(format, size));
            await ReplyAsync("", embed: output.Build());
        }

        [Command("purge")]
        [Summary("Purges an amount of messages in the current channel.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task Purge(int amount)
        {
            try
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                await ReplyAsync($"Deleted {amount} messages.");
            }
            catch
            {
                await ReplyAsync($"Error deleting {amount} messages.");
            }
        }

        [Command("userinfo")]
        [Summary("Get info on the user(s) you list. If no users are provided, get info on yourself.")]
        public async Task UserInfo(params String[] users)
        {
            if (Context.Message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser user in Context.Message.MentionedUsers)
                {
                    SocketGuildUser mentionedUser = Context.Guild.Users.FirstOrDefault(x => x.Username.ToLower() == user.Username.ToLower());
                    if (mentionedUser != null)
                    {
                        await UserInfo(mentionedUser);
                    }
                }
                return;
            }

            if (users.Count() > 1)
            {
                foreach (string user in users)
                {
                    await UserInfo(new String[] { user });
                }
                return;
            }

            // If they don't specify a user, print the info for themselves.
            if(users.Count() == 0)
            {
                users = new string[] { Context.User.Username };
            }

            // Print the info for the wanted user.
            SocketGuildUser wantedUser = Context.Guild.Users.FirstOrDefault(x => x.Username.ToLower() == users[0].ToLower());
            if(wantedUser != null)
            {
                await UserInfo(wantedUser);
            }
        }

        [Command("Say")]
        public async Task Say(params string[] message)
        {
            await Context.Channel.SendMessageAsync(String.Join(' ', message));
        }

        private async Task UserInfo(SocketGuildUser user)
        {
            EmbedBuilder output = new EmbedBuilder();

            DateTimeOffset createdAt = user.CreatedAt;
            DateTimeOffset joinedAt = (DateTimeOffset)user.JoinedAt;
            string nickname = string.IsNullOrEmpty(user.Nickname) ? "" : $"({user.Nickname})";

            output.WithTitle($"{user.Username}#{user.Discriminator} {nickname}")
                .AddField("Status:", $"{user.Status}")
                .AddField("Joined Discord On:", $"{createdAt} ({(DateTime.UtcNow - createdAt).Days} days ago).")
                .AddField("Joined Server On:", $"{joinedAt} ({(DateTime.UtcNow - joinedAt).Days} days ago).")
                .AddField("Roles:", $"{string.Join(", ", user.Roles.ToList())}");

            await ReplyAsync("", embed: output.Build());
        }

        private uint GetRawColor(string color)
        {
            uint argb = 0;
            if(UInt32.TryParse(color.Replace("#", ""), 
                NumberStyles.HexNumber, 
                System.Globalization.CultureInfo.InvariantCulture, out argb)){
                return argb;
            }
            else
            {
                throw new Exception("Invalid hex code.");
            }
        }
    }
}
