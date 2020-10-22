using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;
using TwitchLib.Api;

/// <summary>
/// 
/// </summary>
namespace TidesBotDotNet.Modules
{
    [Group("react")]
    public class ReactionRoleModule : ModuleBase<SocketCommandContext>
    {
        private DiscordSocketClient client;
        private ReactionRoleService roleService;
        private GuildsDefinition guildsDefinition;

        public ReactionRoleModule(DiscordSocketClient client, GuildsDefinition guildsDefinition, ReactionRoleService roleService)
        {
            this.client = client;
            this.guildsDefinition = guildsDefinition;
            this.roleService = roleService;
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Command("add")]
        [Alias("a")]
        [Summary("Makes reacting to the given emote on the given message assign the user a role. Roles within the same group " +
            "mean that the user can't have those roles at the same time.")]
        public async Task OnAddReaction(string messageID, string emote, string role, string group = "")
        {
            Emoji emoji = null;
            // Check that the emote exist.
            if (!Emote.TryParse(emote, out var emoteResult))
            {
                emoji = new Emoji(emote);
            }

            ulong realMessageID = 0;
            // Convert the string to a message ID.
            if (!ulong.TryParse(messageID, out realMessageID))
            {
                await ReplyAsync($"Invalid message ID.");
                return;
            }

            // Check that the message exist.
            IMessage tt = null;
            foreach (SocketTextChannel channel in Context.Guild.TextChannels)
            {
                tt = await channel.GetMessageAsync(realMessageID);
                if(tt != null)
                {
                    break;
                }
            }

            if (tt == null)
            {
                await ReplyAsync($"Could not find message with ID {tt.Content}.");
                return;
            }

            // Check that the role exist.
            SocketRole realRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());

            if(realRole == null)
            {
                await ReplyAsync($"Could not find role {role}.");
                return;
            }

            // Check result.
            string result = await roleService.AddReactRole(Context.Guild, tt, emoteResult, emoji, realRole, group);
            await ReplyAsync(result);

            guildsDefinition.SaveReactRoles();
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Command("remove")]
        [Alias("r")]
        public async Task OnRemoveReaction(string messageID, string emote, string role)
        {
            Emoji emoji = null;
            // Check that the emote exist.
            if (!Emote.TryParse(emote, out var emoteResult))
            {
                emoji = new Emoji(emote);
            }

            ulong realMessageID = 0;
            // Convert the string to a message ID.
            if (!ulong.TryParse(messageID, out realMessageID))
            {
                await ReplyAsync($"Invalid message ID.");
                return;
            }

            // Check that the message exist.
            IMessage tt = null;
            foreach (SocketTextChannel channel in Context.Guild.TextChannels)
            {
                tt = await channel.GetMessageAsync(realMessageID);
                if (tt != null)
                {
                    break;
                }
            }

            if (tt == null)
            {
                await ReplyAsync($"Could not find message with ID {tt.Content}.");
                return;
            }

            // Check that the role exist.
            SocketRole realRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());

            if (realRole == null)
            {
                await ReplyAsync($"Could not find role {role}.");
                return;
            }

            // Check result.
            string result = await roleService.RemoveReactRole(Context.Guild, tt, emoteResult, emoji, realRole);
            await ReplyAsync(result);

            guildsDefinition.SaveReactRoles();
        }
    }
}
