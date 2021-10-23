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
using Victoria;

namespace TidesBotDotNet.Modules
{
    [Group("dj")]
    public class DJModule : ModuleBase<SocketCommandContext>
    {
        private DJService djService;
        private DiscordSocketClient client;

        public DJModule(DJService djService, DiscordSocketClient client)
        {
            this.djService = djService;
            this.client = client;
        }

        [Command("join")]
        [Summary("Joins the voice channel.")]
        public async Task Join()
        {
            string result = await djService.JoinChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
            await Context.Message.Channel.SendMessageAsync(result);
        }

        [Command("leave")]
        [Summary("Leave the voice channel.")]
        [Alias("stop")]
        public async Task Disconnect()
        {
            string result = await djService.LeaveChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
            await Context.Message.Channel.SendMessageAsync(result);
        }

        [Command("play")]
        [Summary("Plays the link given.")]
        [Alias("p")]
        public async Task PlayMusic([Remainder]string link)
        {
            string result = await djService.PlayQuery(Context.User.Username.ToLower(), Context.Guild, 
                (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel, link.Trim());
            await Context.Message.Channel.SendMessageAsync(result);
        }

        [Command("queue")]
        [Summary("Show the songs that are coming up next.")]
        public async Task Queue()
        {
            var queue = djService.GetQueue(Context.Guild);

            if(queue.Count == 0)
            {
                await Context.Message.Channel.SendMessageAsync("The queue is empty.");
                return;
            }

            EmbedBuilder output = new EmbedBuilder();

            output.WithTitle("Queue");

            var currentTrack = djService.GetCurrentlyPlaying(Context.Guild);

            output.AddField($"Currently Playing " +
                $"({currentTrack.track.Position.ToString(@"hh\:mm\:ss")}/{currentTrack.track.Duration}) by {currentTrack.username}:", 
                $"[{currentTrack.track.Title}]({currentTrack.track.Url})");

            int trackPosition = 1;
            foreach(var queueItem in queue)
            {
                var track = queueItem.track as LavaTrack;
                output.AddField($"{trackPosition}. {track.Duration} by {currentTrack.username}", $"[{track.Title}]({track.Url})");
                trackPosition++;
            }

            await ReplyAsync("", embed: output.Build());
        }

        [Command("current")]
        [Summary("Shows the song that is currently playing.")]
        [Alias("now")]
        public async Task CurrentSong()
        {
            var currentTrack = djService.GetCurrentlyPlaying(Context.Guild);

            if(currentTrack == null)
            {
                await Context.Channel.SendMessageAsync("No track is currently playing.");
                return;
            }

            await Context.Channel.SendMessageAsync(
                $"Currently Playing `{currentTrack.track.Title}` ({currentTrack.track.Position.ToString(@"hh\:mm\:ss")}" +
                $"/{currentTrack.track.Duration}) by {currentTrack.username}.");
        }

        [Command("remove")]
        [Summary("Removes a entry from the queue.")]
        public async Task Remove(int position)
        {
            var result = djService.RemoveFromQueue(Context.Guild, position-1);

            if (!result)
            {
                await Context.Channel.SendMessageAsync("Could not remove entry from that position.");
                return;
            }
            await Context.Channel.SendMessageAsync($"Removed entry at {position}.");
        }

        [Command("seek")]
        [Summary("Skips to the time given.")]
        public async Task SeekTime(string time)
        {
            TimeSpan convertedTime;
            if (TimeSpan.TryParse(time, out convertedTime))
            {
                var s = await djService.SeekTime(Context.Guild, convertedTime);
                await Context.Channel.SendMessageAsync(s);
            }
            else
            {
                await Context.Channel.SendMessageAsync("Improper time provided.");
            }
        }

        [Command("shuffle")]
        [Summary("Shuffles the queue.")]
        public async Task ShuffleQueue()
        {
            string s = djService.ShuffleQueue(Context.Guild);

            await Context.Channel.SendMessageAsync(s);
        }

        [Command("loop")]
        [Summary("Toggles if the current song should loop.")]
        public async Task ToggleLoop()
        {
            string s = djService.ToggleLoop(Context.Guild);

            await Context.Channel.SendMessageAsync(s);
        }

        [Command("skip")]
        [Summary("Vote to skip the current song.")]
        public async Task SkipSong()
        {
            string result = "Sorry, ane error has occured.";
            try
            {
                result = await djService.VoteToSkip(Context.Guild, Context.User.Username);
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            await Context.Channel.SendMessageAsync(result);
        }

    }
}
