using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using TidesBotDotNet.Services;
using Victoria.Player;

namespace TidesBotDotNet.Modules
{
    [Group("dj", "Music-related commands.")]
    public class DJModule : InteractionModuleBase<SocketInteractionContext>
    {
        private DJService djService;

        public DJModule(DJService djService)
        {
            this.djService = djService;
        }

        [SlashCommand("join", "Joins the voice channel.")]
        public async Task Join()
        {
            string result = await djService.JoinChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync(result, ephemeral: true);
        }

        [SlashCommand("stop", "Stops playback and leaves the voice channel.")]
        public async Task Disconnect()
        {
            string result = await djService.LeaveChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
            await RespondAsync(result, ephemeral: true);
        }

        [SlashCommand("play", "Plays the link given.", runMode: RunMode.Async)]
        public async Task PlayMusic(string link)
        {
            string result = await djService.PlayQuery(Context.User, Context.Guild, 
                (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel, link.Trim());
            await RespondAsync(result);
        }

        //TODO
        public async Task Search(string query)
        {
            await RespondAsync("To be implemented.", ephemeral: true);
        }

        [SlashCommand("queue", "Shows current and upcoming songs.")]
        public async Task Queue()
        {
            var queue = djService.GetQueue(Context.Guild);

            if(queue.Count == 0)
            {
                await RespondAsync("The queue is empty.", ephemeral: true);
                return;
            }

            EmbedBuilder output = new EmbedBuilder();

            output.WithTitle($"Queue ({queue.Count} Entries)");
            output.WithColor(Color.DarkBlue);

            var currentTrack = djService.GetCurrentlyPlaying(Context.Guild);

            output.AddField($"Now Playing: {currentTrack.track.Title}",
                $"[link]({currentTrack.track.Url}) {(currentTrack.track.Duration - currentTrack.track.Position).ToString(@"hh\:mm\:ss")} left [{currentTrack.user.Mention}]");

            int trackPosition = 1;
            foreach(var queueItem in queue)
            {
                var track = queueItem.track as LavaTrack;
                output.AddField($"{trackPosition}. {track.Title}", $"[link]({track.Url}) {track.Duration} [{queueItem.user.Mention}]");
                trackPosition++;
            }

            await RespondAsync("", embed: output.Build());
        }

        [SlashCommand("now", "Shows the current playing song.")]
        public async Task CurrentSong()
        {
            var currentTrack = djService.GetCurrentlyPlaying(Context.Guild);

            if(currentTrack == null)
            {
                await RespondAsync("No track is currently playing.", ephemeral: true);
                return;
            }

            await RespondAsync(
                $"**Currently Playing** 🎶 `{currentTrack.track.Title}` ({currentTrack.track.Position.ToString(@"hh\:mm\:ss")}" +
                $"/{currentTrack.track.Duration}) [{currentTrack.user.Mention}]");
        }

        [SlashCommand("remove", "Removes an entry from the queue.")]
        public async Task Remove(int position)
        {
            var result = djService.RemoveFromQueue(Context.Guild, position-1);

            if (!result)
            {
                await RespondAsync("Could not remove entry from that position.", ephemeral: true);
                return;
            }
            await RespondAsync($"Removed entry at {position}.");
        }

        [SlashCommand("seek", "Skips to the time given.")]
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

        [SlashCommand("shuffle", "Shuffles the queue.")]
        public async Task ShuffleQueue()
        {
            string s = djService.ShuffleQueue(Context.Guild);

            await Context.Channel.SendMessageAsync(s);
        }

        [SlashCommand("loop", "Toggles if the current song should loop.")]
        public async Task ToggleLoop()
        {
            string s = djService.ToggleLoop(Context.Guild);

            await Context.Channel.SendMessageAsync(s);
        }

        [SlashCommand("skip", "Vote to skip the current song.")]
        public async Task SkipSong()
        {
            string result = "Sorry, an error has occured.";
            try
            {
                result = await djService.VoteToSkip(Context.Guild, Context.User.Username);
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                await RespondAsync($"Encountered an error skipping. : {e.ToString}", ephemeral: true);
            }
            await RespondAsync(result);
        }

    }
}
