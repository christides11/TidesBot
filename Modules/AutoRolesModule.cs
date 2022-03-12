using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Services;
using static TidesBotDotNet.Services.AutoRolesService;

namespace TidesBotDotNet.Modules
{
    [Group("autoroles")]
    public class AutoRolesModule : ModuleBase<SocketCommandContext>
    {
        public AutoRolesService autoRolesService;
        private DiscordSocketClient client;

        public AutoRolesModule(AutoRolesService autoRoleService, DiscordSocketClient client)
        {
            this.autoRolesService = autoRoleService;
            this.client = client;
        }

        [Command("roles")]
        [Summary("List the roles that are assigned when a user joins.")]
        public async Task Roles () {
            AutoRolesGuildDefinition arg = autoRolesService.autoRoles.FirstOrDefault(x => x.guildID == Context.Guild.Id);
            if(arg == null) {
                await Context.Channel.SendMessageAsync("No roles are currently being handled.");
                return;
            }
            await Context.Channel.SendMessageAsync(string.Join(',', arg.roles));
        }

        [Command("addrole")]
        [Alias("ar")]
        [Summary("Adds a role to be auto assigned to users.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task AddRole(string role)
        {
            SocketRole wantedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());
            if(wantedRole == null)
            {
                await Context.Channel.SendMessageAsync($"Role {role} not found.");
                return;
            }
            await Context.Channel.SendMessageAsync(autoRolesService.AddRole(Context.Guild, wantedRole));
            autoRolesService.Save();
        }

        [Command("removerole")]
        [Alias("rr")]
        [Summary("Removes a role to be auto assigned to users.")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        public async Task RemoveRole(string role)
        {
            SocketRole wantedRole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());
            if (wantedRole == null)
            {
                await Context.Channel.SendMessageAsync($"Role {role} not found.");
                return;
            }
            await Context.Channel.SendMessageAsync(autoRolesService.RemoveRole(Context.Guild, wantedRole));
            autoRolesService.Save();
        }
    }
}
