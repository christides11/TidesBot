using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ReactionRoleModule(DiscordSocketClient client, ReactionRoleService roleService)
        {
            this.client = client;
            this.roleService = roleService;
        }

        [Command("add")]
        public async Task OnAddReaction(string messageID, string emote, string group = "")
        {
            await Context.Channel.SendMessageAsync("hello.");
            IMessage tt = null;
            ulong realMessageID = 0;

            if (Emote.TryParse(emote, out var e))
            {
                await Context.Message.AddReactionAsync(e);
            }

            if (!ulong.TryParse(messageID, out realMessageID))
            {
                //await ReplyAsync($"Incorrect message ID.");
                return;
            }

            /*
            foreach(SocketTextChannel channel in Context.Guild.TextChannels)
            {
                tt = await channel.GetMessageAsync(realMessageID);
                if(tt != null)
                {
                    break;
                }
            }

            if (tt != null)
            {
                await ReplyAsync($"found message {tt.Content}.");
            }*/
        }

        private async Task HandleEmoji()
        {

        }
    }
}
