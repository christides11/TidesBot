using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Modules
{
    public class UserModule : InteractionModuleBase<SocketInteractionContext>
    {
        public GuildsDefinition guildsDefinition;

        public UserModule(GuildsDefinition gd)
        {
            guildsDefinition = gd;
        }

        [SlashCommand("colorme", "Gives the user a color based on the hexcode given.")]
        public async Task ColorMe(string color)
        {
            // Check if this guild supports ColorMe.
            if (!guildsDefinition.GetSettings(Context.Guild.Id).colorMe)
            {
                await RespondAsync("Sorry, ColorMe is not enabled on this server.", ephemeral: true);
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
                await RespondAsync("Invalid hex code.", ephemeral: true);
                return;
            }

            var currentRole = Context.Guild.GetUser(Context.User.Id).Roles.LastOrDefault(x => x.Name != roleName);
            // Check if the user already has a color role.
            IRole colorRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            // Get where this role should go so it works, which is right above their current role.
            int highestRole = currentRole.Position+1;

            // Create the role if needed.
            if (colorRole == null)
            {
                colorRole = await Context.Guild.CreateRoleAsync(roleName, currentRole.Permissions, roleColor, false, false);
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
            await RespondAsync($"Assigned color {roleColor.ToString()} to {Context.User.Username}.");
        }

        [SlashCommand("colorme-remove", "Removes the color role from the user.")]
        public async Task ColorMeRemove()
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "colorme" + Context.User.Id);
            if(role != null)
            {
                await role.DeleteAsync();
            }
            await RespondAsync("Removed color role.");
        }

        [SlashCommand("colorme-cleanup", "Cleanup any stray colorme roles.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task ColorMeCleanup()
        {
            var roles = Context.Guild.Roles.Where(x => x.Name.ToLower().Contains("colorme"));
            if (roles.Count() == 0)
            {
                await RespondAsync("No stray roles.");
                return;
            }

            string response = "";
            foreach (var role in roles)
            {
                if (role.Members.Count() > 0) continue;
                response += $"{role.Name}\n";
            }
            await RespondAsync($"Stray Roles: {response}");
        }

        [SlashCommand("avatar", "Gets the avatar of the user.")]
        public async Task Avatar([Choice("32x32", 32), Choice("64x64", 64), Choice("128x128", 128), Choice("256x256", 256), Choice("512x512", 512), Choice("1024x1024", 1024), Choice("2048x2048", 2048)]ushort size, SocketUser users)
        {
            ImageFormat format = ImageFormat.Auto;
            await AvatarTask(users, format, size);
        }

        [SlashCommand("fxembed-opt-out", "Opt out of embed link fixing.")]
        public async Task VxOptOut()
        {
            var userID = Context.User.Id;
            // Check if the user is already opted out.
            if (guildsDefinition.GetSettings(Context.Guild.Id).vxLinkOptOut.Contains(userID))
            {
                await RespondAsync("You are already opted out.", ephemeral: true);
                return;
            }

            guildsDefinition.GetSettings(Context.Guild.Id).vxLinkOptOut.Add(userID);
            guildsDefinition.SaveSettings();

            await RespondAsync("Opted out of embed fix.", ephemeral: true);
        }

        [SlashCommand("fxembed-opt-in", "Opt in of embed link fixing.")]
        public async Task VxOptIn()
        {
            var userID = Context.User.Id;
            // Check if the user is already opted in..
            if (!guildsDefinition.GetSettings(Context.Guild.Id).vxLinkOptOut.Contains(userID))
            {
                await RespondAsync("You are already opted in by default.", ephemeral: true);
                return;
            }

            guildsDefinition.GetSettings(Context.Guild.Id).vxLinkOptOut.Remove(userID);
            guildsDefinition.SaveSettings();

            await RespondAsync("Opted in to embed fix.", ephemeral: true);
        }

        public async Task AvatarTask(SocketUser user, ImageFormat format, ushort size)
        {
            EmbedBuilder output = new EmbedBuilder();
            output.WithImageUrl(user.GetAvatarUrl(format, size));
            await RespondAsync("", embed: output.Build());
        }

        [SlashCommand("purge", "Purges an amount of messages in the current channel.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task Purge(int amount)
        {
            try
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                await RespondAsync($"Deleted {amount} messages.");
            }
            catch
            {
                await RespondAsync($"Error deleting {amount} messages.", ephemeral: true);
            }
        }

        [SlashCommand("user-info", "Get info on a given user.")]
        public async Task UserInfo(SocketUser users)
        {
            SocketGuildUser wantedUser = Context.Guild.Users.FirstOrDefault(x => x.Id == users.Id);
            // Print the info for the wanted user.
            if (wantedUser != default(SocketGuildUser))
            {
                await PrintUserInfo(wantedUser);
            }
        }

        [SlashCommand("say", "Give the bot a sentence to say.")]
        public async Task Say(string message)
        {
            
            await RespondAsync(String.Join(' ', message));
        }

        private async Task PrintUserInfo(SocketGuildUser user)
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

            await RespondAsync(embed: output.Build());
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
