using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Modules
{
    [Group("streamrole", "Streamrole-related commands.")]
    public class StreamRoleModule : InteractionModuleBase<SocketInteractionContext>
    {
        public GuildsDefinition guildsDefinition;
        public StreamRoleService streamRoleService;

        public StreamRoleModule(DiscordSocketClient client, GuildsDefinition guildsDefinition, StreamRoleService streamRoleService)
        {
            this.guildsDefinition = guildsDefinition;
            this.streamRoleService = streamRoleService;
        }

        [SlashCommand("set-role", "Set the role used for when members go live.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task SetRole(string roleName)
        {
            if (!GuildValid())
            {
                await RespondAsync("StreamRole is not enabled in this guild.", ephemeral: true);
                return;
            }
            bool result = streamRoleService.SetRole(Context.Guild.Id, roleName);
            if (!result)
            {
                await RespondAsync("Error while setting the role.", ephemeral: true);
                return;
            }
            await RespondAsync($"Successfully set the role to {roleName}.", ephemeral: true);
        }

        [SlashCommand("register", "Register self stream role.")]
        public async Task Register(string twitchUsername)
        {
            if (!GuildValid())
            {
                await RespondAsync("StreamRole is not enabled in this guild.", ephemeral: true);
                return;
            }
            bool result = await streamRoleService.AddUser(Context.Guild.Id, Context.User as SocketGuildUser, twitchUsername);
            if (!result)
            {
                await RespondAsync("Error while trying to register. Please check your input and try again.", ephemeral: true);
                return;
            }
            await RespondAsync("Successfully registered to stream role service.", ephemeral: true);
        }

        [SlashCommand("unregister", "Unregister self stream role.")]
        public async Task Unregister()
        {
            if (!GuildValid())
            {
                await RespondAsync("StreamRole is not enabled in this guild.", ephemeral: true);
                return;
            }
            bool result = streamRoleService.RemoveUser(Context.Guild.Id, Context.User as SocketGuildUser);
            if (!result)
            {
                await RespondAsync("Error while trying to unregister. Please check your input and try again.", ephemeral: true);
                return;
            }
            await RespondAsync("Successfully unregistered to stream role service.", ephemeral: true);
        }

        private bool GuildValid()
        {
            var guild = Context.Guild;
            return guildsDefinition.GetSettings(guild.Id).streamRoles;
        }
    }
}
