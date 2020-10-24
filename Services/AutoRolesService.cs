using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidesBotDotNet.Services
{
    public class AutoRolesService
    {
        public class AutoRolesGuildDefinition
        {
            public ulong guildID;
            public List<string> roles = new List<string>();

            public AutoRolesGuildDefinition(ulong guildID)
            {
                this.guildID = guildID;
                roles = new List<string>();
            }
        }

        private readonly string monitoredUsersFilename = "autoroles.json";

        private DiscordSocketClient client;

        public List<AutoRolesGuildDefinition> autoRoles = new List<AutoRolesGuildDefinition>();

        public AutoRolesService(DiscordSocketClient client)
        {
            this.client = client;
            autoRoles = SaveLoadService.Load<List<AutoRolesGuildDefinition>>(monitoredUsersFilename);
            if(autoRoles == null)
            {
                autoRoles = new List<AutoRolesGuildDefinition>();
                SaveLoadService.Save(monitoredUsersFilename, autoRoles);
            }

            client.UserJoined += OnUserJoinedGuild;
        }

        private async Task OnUserJoinedGuild(SocketGuildUser user)
        {
            AutoRolesGuildDefinition arg = autoRoles.FirstOrDefault(x => x.guildID == user.Guild.Id);
            // Guild doesn't auto assign any roles.
            if(arg == null)
            {
                return;
            }
            // Give the user every role they should have.
            foreach(string role in arg.roles)
            {
                await GiveRole(user, role);
            }
        }

        public async Task GiveRole(SocketGuildUser user, string role)
        {
            SocketRole realRole = user.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == role.ToLower());

            // If the user doesn't have the role, give it to them.
            if (user.Guild.GetUser(user.Id).Roles.FirstOrDefault(x => x.Name == realRole.Name) == null)
            {
                try
                {
                    await user.Guild.GetUser(user.Id).AddRolesAsync(new List<SocketRole>() { realRole });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not give user role! " + e.Message);
                }
            }
        }

        public string AddRole(SocketGuild guild, SocketRole role)
        {
            AutoRolesGuildDefinition arg = autoRoles.FirstOrDefault(x => x.guildID == guild.Id);
            if(arg == null)
            {
                arg = new AutoRolesGuildDefinition(guild.Id);
                autoRoles.Add(arg);
            }
            if(!arg.roles.Contains(role.Name.ToLower()))
            {
                arg.roles.Add(role.Name.ToLower());
                return $"Role {role.Name} added.";
            }

            return $"{role.Name} is already in the list.";
        }

        public string RemoveRole(SocketGuild guild, SocketRole role)
        {
            AutoRolesGuildDefinition arg = autoRoles.FirstOrDefault(x => x.guildID == guild.Id);
            if (arg == null)
            {
                return "No roles being assigned in this guild.";
            }
            if (arg.roles.Contains(role.Name.ToLower()))
            {
                arg.roles.Remove(role.Name.ToLower());
                return $"Role {role.Name} removed.";
            }
            return $"Role {role.Name} is not in the list.";
        }

        public void Save()
        {
            SaveLoadService.Save(monitoredUsersFilename, autoRoles);
        }
    }
}
