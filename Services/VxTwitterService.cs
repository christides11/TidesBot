using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Services
{
    public class VxTwitterService
    {
        private DiscordSocketClient client;
        public GuildsDefinition guildsDefinition;

        public VxTwitterService(DiscordSocketClient client, GuildsDefinition gd)
        {
            Console.WriteLine("VX TWITTER SERVICE STARTED");
            this.client = client;
            this.guildsDefinition = gd;
            client.MessageReceived += WhenMessageSent;
        }

        private async Task WhenMessageSent(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            var chnl = msg.Channel as SocketGuildChannel;
            var Guild = chnl.Guild;

            if (!guildsDefinition.GetSettings(Guild.Id).vxLinks) return;
            if (!msg.Content.Contains("https://twitter.com") && !msg.Content.Contains("https://x.com")
                && !msg.Content.Contains("https://www.twitter.com") && !msg.Content.Contains("https://www.x.com")) return;
            if (msg.Author.IsBot) return;

            var msgContent = msg.Content;
            var msgUsername = msg.Author.Username;
            var msgAvatar = msg.Author.GetAvatarUrl();
            msgContent = msgContent.Replace("www.", "");
            msgContent = msgContent.Replace("https://twitter.com", "https://vxtwitter.com");
            msgContent = msgContent.Replace("https://x.com", "https://fixvx.com");
            await msg.DeleteAsync();

            RestWebhook wh = await CreateOrGetWebhook(chnl);

            var DCW = new DiscordWebhookClient(wh);
            using (var client = DCW)
            {
                await client.SendMessageAsync($"{msgContent}", username: msgUsername, avatarUrl: msgAvatar);
            }
        }

        private static async Task<RestWebhook> CreateOrGetWebhook(SocketGuildChannel chnl)
        {
            var whs = await (chnl as SocketTextChannel).GetWebhooksAsync();
            var wh = whs.Where(x => x.Name == "vxtwit").FirstOrDefault();
            if (wh == null || wh == default(RestWebhook)) wh = await (chnl as SocketTextChannel).CreateWebhookAsync("vxtwit");
            return wh;
        }
    }
}
