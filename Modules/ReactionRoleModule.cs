using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Modules
{
    [Group("react", "Commands related to reaction roles.")]
    public class ReactionRoleModule : InteractionModuleBase<SocketInteractionContext>
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

        [SlashCommand("refresh", "Refreshes the role service for this guild.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task RefreshReactions()
        {
            await roleService.RefreshRoles(Context.Guild);
        }

        [SlashCommand("add", "Makes reacting to the given emote on the given message assign the user a role.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
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
                await RespondAsync($"Invalid message ID.", ephemeral: true);
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
                await RespondAsync($"Could not find message with ID {tt.Content}.", ephemeral: true);
                return;
            }

            // Check that the role exist.
            SocketRole realRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());

            if(realRole == null)
            {
                await RespondAsync($"Could not find role {role}.", ephemeral: true);
                return;
            }

            // Check result.
            string result = await roleService.AddReactRole(Context.Guild, tt, emoteResult, emoji, realRole, group);
            await RespondAsync(result, ephemeral: true);

            guildsDefinition.SaveReactRoles();
        }

        [SlashCommand("remove", "Removes a given reaction role.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
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
