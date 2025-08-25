using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Services
{
    public class VxTwitterService
    {
        private DiscordSocketClient client;
        public GuildsDefinition guildsDefinition;

        // User Message ID : Bot Message ID
        private ConcurrentDictionary<ulong, ulong> lastVxedMessages = new ConcurrentDictionary<ulong, ulong>();
        // Bot Message ID : Bot Message
        private ConcurrentDictionary<ulong, IMessage> messageBuffer = new ConcurrentDictionary<ulong, IMessage>();
        private List<ulong> messageQueue = new List<ulong>();

        public VxTwitterService(DiscordSocketClient client, GuildsDefinition gd)
        {
            this.client = client;
            this.guildsDefinition = gd;
            client.MessageReceived += WhenMessageSent;
            client.MessageDeleted += WhenMessageDeleted;
        }

        private async Task WhenMessageDeleted(Cacheable<IMessage, ulong> cacheable1, Cacheable<IMessageChannel, ulong> cacheable2)
        {
            var msgID = cacheable1.Id;
            if (!lastVxedMessages.ContainsKey(msgID)) return;
            var botMsgID = lastVxedMessages[msgID];
            if (!messageBuffer.ContainsKey(botMsgID)) return;

            try
            {
                await messageBuffer[botMsgID].DeleteAsync();
            }catch (Exception ex)
            {
                Console.WriteLine($"Could not delete old message. {ex}");
            }

            lastVxedMessages.Remove(msgID, out var outp);
            messageBuffer.Remove(botMsgID, out var mss);
            messageQueue.Remove(msgID);
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
                    || msg.Content.Length >= 500
                    || guildSettings.IsUserOptedOutOfXV(msg.Author.Id)
                    || (!msg.Content.Contains(".com") && !msg.Content.Contains(".app"))
                    || !msg.Content.Contains("https://")) return;

                var msgContent = msg.Content;
                msgContent = msgContent.Replace("www.", "");

                var scTwitter = StringContainsTwitter(msgContent);
                var scInstagram = StringContainsInstagram(msgContent);
                var scTiktok = StringContainsTiktok(msgContent);
                var scShortTiktok = StringContainsShortTiktok(msgContent);
                var scBlueSky = StringContainsBluesky(msgContent);

                if (scTwitter && !guildSettings.vxTwitter && !guildSettings.fxTwitter
                    || scInstagram && !guildSettings.vxInstagram
                    || scTiktok && !guildSettings.vxTiktok
                    || scShortTiktok && !guildSettings.vxShortTiktok
                    || scBlueSky && !guildSettings.vxBlueSky) return;
                if (!scTwitter && !scInstagram && !scTiktok && !scShortTiktok && !scBlueSky) return;

                msgContent = GetVXedLink(msg.Author, msgContent, out var UNick, out var msgAvatar, guildSettings.fxTwitter);

                var partsOfString = msgContent.Replace("\n", " ").Split(" ").Where(s => s.Contains("https")).ToArray();

                var stringWithOnlyLinks = "";

                for(int i = 0; i < partsOfString.Length; i++)
                {
                    var rawLink = UnVXLink(partsOfString[i]);

                    if (IsLinkTwitter(rawLink))
                    {
                        stringWithOnlyLinks += $"<{rawLink}> [embed]({partsOfString[i]}) [xcancel](<{XCancelLink(rawLink)}>)";
                    }
                    else
                    {
                        stringWithOnlyLinks += $"<{rawLink}> [embed]({partsOfString[i]})";
                    }
                    if (i < partsOfString.Length - 1) stringWithOnlyLinks += "\n";
                }

                if (string.IsNullOrEmpty(stringWithOnlyLinks)) return;

                var botMessage = await msg.Channel.SendMessageAsync(stringWithOnlyLinks);
                //await msg.ModifyAsync(p => p.Flags = MessageFlags.SuppressEmbeds);
                _ = ForceNoEmbed(msg);

                TryCleanupMessageQueue();

                lastVxedMessages.TryAdd(msg.Id, botMessage.Id);
                messageBuffer.TryAdd(botMessage.Id, botMessage);
                messageQueue.Add(msg.Id);

            }catch(Exception e)
            {
                Console.WriteLine($"Error when trying to Vx link: {e}");
            }
        }

        private async Task ForceNoEmbed(SocketUserMessage msg)
        {
            int attempts = 10;
            while (attempts > 0)
            {
                await msg.ModifyAsync(p => p.Flags = MessageFlags.SuppressEmbeds);
                await Task.Delay(500);
                attempts--;
            }
        }

        private void TryCleanupMessageQueue()
        {
            if (messageQueue.Count <= 30) return;

            if (lastVxedMessages.TryGetValue(messageQueue[0], out ulong botMessageID))
            {
                lastVxedMessages.Remove(messageQueue[0], out var outVal);

                if(messageBuffer.TryGetValue(botMessageID, out var message))
                {
                    messageBuffer.Remove(botMessageID, out var v);
                }
            }
            messageQueue.RemoveAt(0);
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
            msgContent = msgContent.Replace("https://bsky.app/", "https://cbsky.app/");

            return msgContent;
        }

        public static string UnVXLink(string msgContent)
        {
            msgContent = msgContent.Replace("https://fixvx.com", "https://x.com");
            msgContent = msgContent.Replace("https://fixupx.com", "https://x.com");
            msgContent = msgContent.Replace("https://ddinstagram.com", "https://instagram.com");
            msgContent = msgContent.Replace("https://vxtiktok.com", "https://tiktok.com");
            msgContent = msgContent.Replace("https://vm.tiktxk.com", "https://vm.tiktok.com");
            msgContent = msgContent.Replace("https://cbsky.app/", "https://bsky.app/");
            return msgContent;
        }

        public static string XCancelLink(string msgContent)
        {
            msgContent = msgContent.Replace("https://x.com", "https://xcancel.com");
            return msgContent;
        }

        public static bool IsLinkTwitter(string linkString)
        {
            return linkString.Contains("https://x.com");
        }

        public static async Task<RestWebhook> CreateOrGetWebhook(SocketGuildChannel chnl)
        {
            var whs = await (chnl as SocketTextChannel).GetWebhooksAsync();
            var wh = whs.Where(x => x.Name == "vxtwit").FirstOrDefault();
            if (wh == null || wh == default(RestWebhook)) wh = await (chnl as SocketTextChannel).CreateWebhookAsync("vxtwit");
            return wh;
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

        bool StringContainsBluesky(string msg)
        {
            if (msg.Contains("https://bsky.app/")) return true;
            return false;
        }
    }
}
