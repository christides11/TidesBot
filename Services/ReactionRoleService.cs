using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TidesBotDotNet.Services
{
    public class ReactionRoleService
    {
        private DiscordSocketClient client;

        public ReactionRoleService(DiscordSocketClient client)
        {
            this.client = client;
            client.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel after, SocketReaction reaction)
        {

        }
    }
}
