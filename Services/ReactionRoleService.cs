using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Services
{
    public class ReactionRoleService
    {
        private DiscordSocketClient client;
        private GuildsDefinition guildsDefinition;

        public ReactionRoleService(DiscordSocketClient client, GuildsDefinition guildsDefinition)
        {
            this.client = client;
            this.guildsDefinition = guildsDefinition;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
            client.ReactionsCleared += ReactionCleared;
        }

        private async Task ReactionCleared(Cacheable<IUserMessage, ulong> before, Cacheable<IMessageChannel, ulong> after)
        {

        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> before, Cacheable<IMessageChannel, ulong> after, SocketReaction reaction)
        {
            var t = Task.Factory.StartNew(async () =>
            {
                if (reaction.User.Value.IsBot)
                {
                    return;
                }

                var chn = reaction.Channel as SocketGuildChannel;

                if (!guildsDefinition.reactRoles.ContainsKey(chn.Guild.Id))
                {
                    return;
                }

                // Check if the message exist.
                bool foundMessage = false;
                ReactRolesDefinition rrDef = null;
                string msgGroup = "";
                foreach (string group in guildsDefinition.reactRoles[chn.Guild.Id].Keys)
                {
                    var x = guildsDefinition.reactRoles[chn.Guild.Id][group].FirstOrDefault(x => x.messageID == reaction.MessageId
                    && CompareEmote(x.emoji, reaction.Emote.ToString()));
                    if (x != null)
                    {
                        foundMessage = true;
                        msgGroup = group;
                        rrDef = x;
                        break;
                    }
                }
                if (!foundMessage)
                {
                    return;
                }

                // REMOVE ROLE //
                SocketRole realRole = chn.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == rrDef.role.ToLower());
                if (realRole != null)
                {
                    await RemoveRole(realRole, chn.Guild, reaction.UserId);
                }
            });
        }

        private bool CompareEmote(string emoji, string gotEmote)
        {
            if(emoji == gotEmote)
            {
                return true;
            }
            string aEmoteGot = emoji;
            if(emoji[1] == 'a')
            {
                emoji = emoji.Remove(1,1);
            }
            if(emoji == gotEmote)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// When a user adds an reaction, check if we have to assign a role for that assignment.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> before, Cacheable<IMessageChannel, ulong> after, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
            {
                return;
            }
            var chn = reaction.Channel as SocketGuildChannel;

            if (!guildsDefinition.reactRoles.ContainsKey(chn.Guild.Id))
            {
                return;
            }

            // Check if the message exist.
            bool foundMessage = false;
            ReactRolesDefinition rrDef = null;
            string msgGroup = "";
            foreach (string group in guildsDefinition.reactRoles[chn.Guild.Id].Keys)
            {
                if (guildsDefinition.reactRoles[chn.Guild.Id][group].FirstOrDefault(x => x.messageID == reaction.MessageId
                && x.emoji == reaction.Emote.ToString()) != null)
                {
                    foundMessage = true;
                    msgGroup = group;
                    rrDef = guildsDefinition.reactRoles[chn.Guild.Id][group].FirstOrDefault(x => x.messageID == reaction.MessageId
                        && x.emoji == reaction.Emote.ToString());
                    break;
                }
            }
            if (!foundMessage)
            {
                return;
            }

            // ASSIGNING ROLES //
            if (string.IsNullOrEmpty(msgGroup))
            {
                // No group assigned, so don't bother trying to check other groups.
                SocketRole realRole = chn.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == rrDef.role.ToLower());
                if (realRole != null)
                {
                    await AssignRole(realRole, chn.Guild, reaction.UserId);
                }
            }
            else
            {
                // Group assigned, make sure nothing else in the group is assigned.
                for (int i = 0; i < guildsDefinition.reactRoles[chn.Guild.Id][msgGroup].Count; i++)
                {
                    if (chn.Guild.GetUser(reaction.UserId).Roles.FirstOrDefault(x => x.Name == guildsDefinition.reactRoles[chn.Guild.Id][msgGroup][i].role) != null)
                    {
                        var m = await reaction.Channel.SendMessageAsync($"{reaction.User.Value.Mention}, you can only have one role within group {msgGroup}.");
                        _ = Clean(m);
                        return;
                    }
                }

                // Assign role.
                SocketRole realRole = chn.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == rrDef.role.ToLower());
                if (realRole != null)
                {
                    await AssignRole(realRole, chn.Guild, reaction.UserId);
                }
            }
        }

        public async Task RefreshRoles(SocketGuild guild)
        {
            if (!guildsDefinition.reactRoles.ContainsKey(guild.Id))
            {
                return;
            }

            foreach (string group in guildsDefinition.reactRoles[guild.Id].Keys)
            {
                //Emote emote = 
                //await message.AddReactionAsync(emote);
                /*
                if (guildsDefinition.reactRoles[guild.Id][group].FirstOrDefault(x => x.messageID == reaction.MessageId
                && x.emoji == reaction.Emote.ToString()) != null)
                {
                    foundMessage = true;
                    msgGroup = group;
                    rrDef = guildsDefinition.reactRoles[guild.Id][group].FirstOrDefault(x => x.messageID == reaction.MessageId
                        && x.emoji == reaction.Emote.ToString());
                    break;
                }*/
            }
        }

        private async Task AssignRole(SocketRole role, SocketGuild guild, ulong userID)
        {
            // If the user doesn't have the role, give it to them.
            if (guild.GetUser(userID).Roles.FirstOrDefault(x => x.Name == role.Name) == null)
            {
                try
                {
                    await guild.GetUser(userID).AddRolesAsync(new List<SocketRole>() { role });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not give user react role! " + e.Message);
                }
            }
        }

        private async Task RemoveRole(SocketRole role, SocketGuild guild, ulong userID)
        {
            // If the user doesn't have the role, give it to them.
            if (guild.GetUser(userID).Roles.FirstOrDefault(x => x.Name == role.Name) != null)
            {
                try
                {
                    await guild.GetUser(userID).RemoveRolesAsync(new List<SocketRole>() { role });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not remove user react role! " + e.Message);
                }
            }
        }

        public async Task Clean(RestUserMessage message)
        {
            await Task.Delay(5000);
            await message.DeleteAsync();
        }

        public async Task<string> AddReactRole(SocketGuild guild, IMessage message, Emote emote, Emoji emoji, SocketRole role, string group = "")
        {
            if (emote != null)
            {
                await message.AddReactionAsync(emote);
            }
            else
            {
                await message.AddReactionAsync(emoji);
            }


            if (!guildsDefinition.reactRoles.ContainsKey(guild.Id))
            {
                guildsDefinition.reactRoles.Add(guild.Id, new Dictionary<string, List<ReactRolesDefinition>>());
            }

            if (!guildsDefinition.reactRoles[guild.Id].ContainsKey(group))
            {
                guildsDefinition.reactRoles[guild.Id].Add(group, new List<ReactRolesDefinition>());
            }

            if (guildsDefinition.reactRoles[guild.Id][group].FirstOrDefault(x => x.messageID == message.Id && x.role == role.Name
             && (x.emoji == emote.ToString() || x.emoji == emoji.ToString())) != null)
            {
                return "Role assignment already exist on that message for that emote.";
            }

            guildsDefinition.reactRoles[guild.Id][group].Add(new ReactRolesDefinition(message.Id, emote != null ? emote.ToString() : emoji.ToString(), role.Name));



            if (emote != null)
            {
                return $"Got it! {emote.ToString()} will assign to role {role.Name}.";
            }
            else
            {
                return $"Got it! {emoji.ToString()} will assign to role {role.Name}.";
            }
        }

        public async Task<string> RemoveReactRole(SocketGuild guild, IMessage message, Emote emote, Emoji emoji, SocketRole role)
        {
            if (!guildsDefinition.reactRoles.ContainsKey(guild.Id))
            {
                return "No react roles for this guild.";
            }

            foreach (string group in guildsDefinition.reactRoles[guild.Id].Keys)
            {
                string e = emote != null ? emote.ToString() : (emoji != null ? emoji.ToString() : null);
                if(e == null)
                {
                    return "Invalid emote.";
                }
                ReactRolesDefinition rrd = guildsDefinition.reactRoles[guild.Id][group].FirstOrDefault(x => x.messageID == message.Id && x.role == role.Name
                    && (x.emoji == e));
                if (rrd != null)
                {
                    guildsDefinition.reactRoles[guild.Id][group].Remove(rrd);
                    await message.RemoveReactionAsync(emote != null ? (IEmote)emote : (IEmote)emoji, client.CurrentUser);
                    return $"Removed reaction role {role.Name}.";
                }
            }

            return $"Reaction for role {role} not found with given parameters.";
        }
    }
}
