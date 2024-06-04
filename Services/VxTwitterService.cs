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
                var guildSettings = guildsDefinition.GetSettings(Guild.Id);

                if (msg.Author.IsBot
                    || msg.MentionedUsers.Count > 0 
                    || msg.MentionedRoles.Count > 0
                    || msg.Reference != null
                    || msg.Content.Length >= 500
                    || guildSettings.IsUserOptedOutOfXV(msg.Author.Id)
                    || !msg.Content.Contains(".com")
                    || !msg.Content.Contains("https://")) return;

                var msgContent = msg.Content;
                msgContent = msgContent.Replace("www.", "");

                var scTwitter = StringContainsTwitter(msgContent);
                var scInstagram = StringContainsInstagram(msgContent);
                var scTiktok = StringContainsTiktok(msgContent);
                var scShortTiktok = StringContainsShortTiktok(msgContent);

                if (scTwitter && !guildSettings.vxTwitter && !guildSettings.fxTwitter
                    || scInstagram && !guildSettings.vxInstagram
                    || scTiktok && !guildSettings.vxTiktok
                    || scShortTiktok && !guildSettings.vxShortTiktok) return;

                if (!scTwitter && !scInstagram && !scTiktok && !scShortTiktok) return;

                msgContent = GetVXedLink(msg.Author, msgContent, out var UNick, out var msgAvatar, guildSettings.fxTwitter);
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

        public static string GetVXedLink(SocketUser user, string msgContent, out string userNickname, out string userAvatarURL, bool useFxInstead = false)
        {
            userNickname = (user as SocketGuildUser).Nickname == null ? user.Username : (user as SocketGuildUser).Nickname;
            userAvatarURL = user.GetAvatarUrl();
            msgContent = msgContent.Replace("https://twitter.com", "https://x.com");
            if(!useFxInstead) msgContent = msgContent.Replace("https://x.com", "https://fixvx.com");
            else msgContent = msgContent.Replace("https://x.com", "https://fixupx.com");
            msgContent = msgContent.Replace("https://instagram.com", "https://ddinstagram.com");
            msgContent = msgContent.Replace("https://tiktok.com", "https://vxtiktok.com");
            msgContent = msgContent.Replace("https://vm.tiktok.com", "https://vm.tiktxk.com");

            return msgContent;
        }

        public static async Task<RestWebhook> CreateOrGetWebhook(SocketGuildChannel chnl)
        {
            var whs = await (chnl as SocketTextChannel).GetWebhooksAsync();
            var wh = whs.Where(x => x.Name == "vxtwit").FirstOrDefault();
            if (wh == null || wh == default(RestWebhook)) wh = await (chnl as SocketTextChannel).CreateWebhookAsync("vxtwit");
            return wh;
        }

        bool StringContainsAnyValid(string msg)
        {
            if(StringContainsTwitter(msg)
                || StringContainsInstagram(msg)
                || StringContainsTiktok(msg)
                || StringContainsShortTiktok(msg)) return true;
            return false;
        }

        bool StringContainsTwitter(string msg)
        {
            if ((msg.Contains("https://twitter.com") || msg.Contains("https://x.com"))
                && msg.Contains("status")) return true;
            return false;
        }

        bool StringContainsInstagram(string msg)
        {
            if (msg.Contains("https://instagram.com")
                && (msg.Contains("reel/") || msg.Contains("reels/") || msg.Contains("/p/") ) ) return true;
            return false;
        }

        bool StringContainsTiktok(string msg)
        {
            if (msg.Contains("https://tiktok.com") && msg.Contains("video")) return true;
            return false;
        }

        bool StringContainsShortTiktok(string msg)
        {
            if (msg.Contains("https://vm.tiktok.com")) return true;
            return false;
        }
    }
}
