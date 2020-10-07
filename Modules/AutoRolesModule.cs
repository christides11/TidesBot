using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Modules
{
    [Group("autoroles")]
    public class AutoRolesModule : ModuleBase<SocketCommandContext>
    {
        public static AutoRolesService autoRolesService;
        private DiscordSocketClient client;

        public AutoRolesModule(DiscordSocketClient client)
        {
            if(autoRolesService == null)
            {
                autoRolesService = new AutoRolesService(client);
            }
            this.client = client;
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
