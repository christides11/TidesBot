using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TidesBotDotNet.Services;
using TwitchLib.Api.Helix.Models.Soundtrack;
using static TidesBotDotNet.Services.DJService;

namespace TidesBotDotNet.Modules
{
    /*
    [Group("dj", "Music-related commands.")]
    public class DJModule : InteractionModuleBase<SocketInteractionContext>
    {
        private DJService djService;
        private Fergun.Interactive.InteractiveService interactiveService;

        public DJModule(DJService djService, Fergun.Interactive.InteractiveService interactiveService)
        {
            this.djService = djService;
            this.interactiveService = interactiveService;
        }

        [SlashCommand("join", "Joins the voice channel.")]
        public async Task Join()
        {
            var result = await djService.JoinChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
            if(result.Item2 != null)
            {
                await RespondAsync($"Exception occured while trying to join voice channel: {result.Item2}");
                return;
            }
            await RespondAsync(result.Item1, ephemeral: true);
        }

        [SlashCommand("play", "Plays the link given.", runMode: RunMode.Async)]
        public async Task PlayMusic(string query, SearchMode searchMode)
        {
            var result = await djService.PlayQuery(Context.User, Context.Guild, (Context.User as IVoiceState).VoiceChannel, query, searchMode);
            if (result.Item2 != null)
            {
                await RespondAsync($"Exception occured while trying to play {query}:{searchMode}: {result.Item2}");
                return;
            }
            await RespondAsync(result.Item1);
        }

        [SlashCommand("stop", "Stops playback and leaves the voice channel.")]
        public async Task Disconnect()
        {
            var result = await djService.LeaveChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
            if (result.Item2 != null)
            {
                await RespondAsync($"Exception occured while trying to leave voice channel: {result.Item2}");
                return;
            }
            await RespondAsync(result.Item1);
        }

        [SlashCommand("queue", "Shows current and upcoming songs.")]
        public async Task Queue()
        {
            var queue = djService.GetQueue(Context.Guild);

            if (queue == null || queue.Count == 0)
            {
                await RespondAsync("The queue is empty.", ephemeral: true);
                return;
            }

            List<PageBuilder> pages = new List<PageBuilder>();
            for(int pageIdx = 0; pageIdx < ((queue.Count)/10)+1; pageIdx++)
            {
                int queuePos = pageIdx * 10;
                if (queuePos >= queue.Count) break;
                pages.Add(new PageBuilder());
                for(int j = pageIdx * 10; j < (pageIdx*10)+10; j++)
                {
                    if (j >= queue.Count) break;
                    var context = (TrackContext)queue[j].Context!;
                    pages[pageIdx].AddField($"{j+1}. {queue[j].Title}", $"[link]({queue[j].Source}) {queue[j].Duration} [{context.RequesterName}]");
                }
            }

            var paginator = new StaticPaginatorBuilder()
                .WithPages(pages) // Set the pages the paginator will use. This is the only required component.
                .Build();

            await RespondAsync($"Showing Song Queue at {DateTime.UtcNow.ToString()} UTC.", ephemeral: true);
            await interactiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));
        }

        [SlashCommand("now", "Shows the current playing song.")]
        public async Task CurrentSong()
        {
            var currentTrack = djService.GetCurrentlyPlaying(Context.Guild);

            if (currentTrack == null)
            {
                await RespondAsync("No track is currently playing.", ephemeral: true);
                return;
            }

            var currentPosition = djService.GetCurrentPosition(Context.Guild);
            var context = (TrackContext)currentTrack.Context!;

            await RespondAsync(
                $"**Currently Playing** 🎶 `{currentTrack.Title}` ({currentPosition.Position.ToString(@"hh\:mm\:ss")}" +
                $"/{currentTrack.Duration}) [{context.RequesterName}]");
        }

        [SlashCommand("remove", "Removes an entry from the queue.")]
        public async Task Remove(int position)
        {
            var result = djService.RemoveFromQueue(Context.Guild, position - 1);

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
                if(s.Item2 != null)
                {
                    await RespondAsync($"Encountered error while seeking: {s.Item2.ToString()}", ephemeral: true);
                    return;
                }
                await RespondAsync(s.Item1);
            }
            else
            {
                await RespondAsync("Improper time provided.", ephemeral: true);
            }
        }

        [SlashCommand("shuffle", "Shuffles the queue.")]
        public async Task ShuffleQueue()
        {
            var r = await djService.ShuffleQueue(Context.Guild);
            if(r.Item2 != null)
            {
                await RespondAsync($"Exception occured when trying to shuffle: {r.Item2.ToString()}", ephemeral: true);
                return;
            }
            await RespondAsync(r.Item1);
        }

        [SlashCommand("loop", "Toggles if the current song should loop.")]
        public async Task ToggleLoop()
        {
            var r = await djService.ToggleLoop(Context.Guild);
            if (r.Item2 != null)
            {
                await RespondAsync($"Exception occured when trying to toggle looping: {r.Item2.ToString()}", ephemeral: true);
                return;
            }
            await RespondAsync(r.Item1);
        }

        [SlashCommand("vote-skip", "Vote to skip the current song.")]
        public async Task SkipSong()
        {
            var r = await djService.VoteToSkip(Context.Guild, Context.User);
            if (r.Item2 != null)
            {
                await RespondAsync($"Exception occured when trying to vote skip: {r.Item2.ToString()}", ephemeral: true);
                return;
            }
            await RespondAsync(r.Item1);
        }

        [SlashCommand("force-skip", "Force skip the song")]
        public async Task ForceSkip()
        {
            var r = await djService.Skip(Context.Guild, Context.User);
            if (r.Item2 != null)
            {
                await RespondAsync($"Exception occured when trying to force skip: {r.Item2.ToString()}", ephemeral: true);
                return;
            }
            await RespondAsync(r.Item1);
        }

        //TODO
        public async Task Search(string query)
        {
            await RespondAsync("To be implemented.", ephemeral: true);
        }
    }*/
}
