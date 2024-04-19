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
            this.client = client;
            this.guildsDefinition = gd;
            client.MessageReceived += WhenMessageSent;
        }

        private async Task WhenMessageSent(SocketMessage arg)
        {
            try
            {
                var msg = arg as SocketUserMessage;
                var chnl = msg.Channel as SocketGuildChannel;
                var Guild = chnl.Guild;

                if (msg.Author.IsBot) return;
                if (!guildsDefinition.GetSettings(Guild.Id).vxLinks) return;
                if (guildsDefinition.GetSettings(Guild.Id).IsUserOptedOut(msg.Author.Id)) return;
                if (!msg.Content.Contains("https://twitter.com") && !msg.Content.Contains("https://x.com")
                    && !msg.Content.Contains("https://www.twitter.com") && !msg.Content.Contains("https://www.x.com")
                    && !msg.Content.Contains("https://www.instagram.com") && !msg.Content.Contains("https://instagram.com")
                    && !msg.Content.Contains("https://www.tiktok.com") && !msg.Content.Contains("https://tiktok.com")
                    && !msg.Content.Contains("https://vm.tiktok.com") ) return;

                var msgContent = msg.Content;
                msgContent = msgContent.Replace("www.", "");
                if (!msgContent.Contains("status") && (msg.Content.Contains("https://x.com") || msg.Content.Contains("https://twitter.com"))) return;

                var UNick = (msg.Author as SocketGuildUser).Nickname == null ? msg.Author.Username : (msg.Author as SocketGuildUser).Nickname;
                var msgAvatar = msg.Author.GetAvatarUrl();
                msgContent = msgContent.Replace("https://x.com", "https://twitter.com");
                msgContent = msgContent.Replace("https://twitter.com", "https://vxtwitter.com");
                msgContent = msgContent.Replace("https://instagram.com", "https://ddinstagram.com");
                msgContent = msgContent.Replace("https://tiktok.com", "https://vxtiktok.com");
                msgContent = msgContent.Replace("https://vm.tiktok.com", "https://vm.tiktxk.com");
                await msg.DeleteAsync();

                RestWebhook wh = await CreateOrGetWebhook(chnl);

                var DCW = new DiscordWebhookClient(wh);
                using (var client = DCW)
                {
                    await client.SendMessageAsync($"{msgContent}", username: UNick, avatarUrl: msgAvatar);
                }
            }catch(Exception e)
            {
                Console.WriteLine($"Error when trying to Vx link: {e.Message}");
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
