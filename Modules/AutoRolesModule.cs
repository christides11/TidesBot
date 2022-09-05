using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Services;
using static TidesBotDotNet.Services.AutoRolesService;

namespace TidesBotDotNet.Modules
{
    [Group("autoroles", "Module for assigning roles when joining guild.")]
    public class AutoRolesModule : InteractionModuleBase<SocketInteractionContext>
    {
        public AutoRolesService autoRolesService;

        public AutoRolesModule(AutoRolesService autoRoleService)
        {
            this.autoRolesService = autoRoleService;
        }

        [SlashCommand("roles", "List the roles that are assigned when a user joins.")]
        public async Task Roles () {
            AutoRolesGuildDefinition arg = autoRolesService.autoRoles.FirstOrDefault(x => x.guildID == Context.Guild.Id);
            if(arg == null) {
                await RespondAsync("No roles are currently being handled.", ephemeral: true);
                return;
            }
            await RespondAsync(string.Join(',', arg.roles), ephemeral: true);
        }

        [SlashCommand("add-role", "Adds a role to be auto assigned to users.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task AddRole(string role)
        {
            SocketRole wantedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());
            if(wantedRole == null)
            {
                await RespondAsync($"Role {role} not found.", ephemeral: true);
                return;
            }
            await RespondAsync(autoRolesService.AddRole(Context.Guild, wantedRole), ephemeral: true);
            autoRolesService.Save();
        }

        [SlashCommand("remove-role", "Removes a role to be auto assigned to users.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task RemoveRole(string role)
        {
            SocketRole wantedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());
            if (wantedRole == null)
            {
                await RespondAsync($"Role {role} not found.", ephemeral: true);
                return;
            }
            await RespondAsync(autoRolesService.RemoveRole(Context.Guild, wantedRole), ephemeral: true);
            autoRolesService.Save();
        }
    }
}
