using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using Victoria;
using Victoria.EventArgs;

namespace TidesBotDotNet.Services
{
    public class DJService
    {
        public class DJGuildInfo
        {
            public bool loopSong = false;
            public List<string> skipVotes = new List<string>();
            public IVoiceChannel voiceChannel;
            public List<DJQueueItem> queue = new List<DJQueueItem>();
            public DJQueueItem currentlyPlaying;

            public DJGuildInfo(IVoiceChannel voiceChannel)
            {
                this.voiceChannel = voiceChannel;
            }

            public void OnNextTrack()
            {
                skipVotes = new List<string>();
                loopSong = false;
                PlayNext();
            }

            public void Enqueue(string username, LavaTrack track)
            {
                queue.Add(new DJQueueItem(username, track));
            }

            public DJQueueItem Dequeue()
            {
                if(queue.Count == 0)
                {
                    return null;
                }
                DJQueueItem item = queue[0];
                queue.RemoveAt(0);
                return item;
            }

            public void PlayNext()
            {
                currentlyPlaying = Dequeue();
            }

            public void Shuffle()
            {
                queue.Shuffle();
            }
        }

        /// <summary>
        /// Represents an item in the music queue. Has the user that requested the track
        /// and the track itself.
        /// </summary>
        public class DJQueueItem
        {
            public string username;
            public LavaTrack track;

            public DJQueueItem(string username, LavaTrack track)
            {
                this.username = username;
                this.track = track;
            }
        }

        private LavaConfig lavaConfig;
        private LavaNode lavaNode;

        private ConcurrentDictionary<IGuild, DJGuildInfo> guildInfo = new ConcurrentDictionary<IGuild, DJGuildInfo>();

        public DJService(LavaConfig lavaConfig, LavaNode lavaNode)
        {
            this.lavaConfig = lavaConfig;
            this.lavaNode = lavaNode;
            this.lavaNode.OnTrackEnded += OnTrackEndedAsync;
            this.lavaNode.OnTrackStuck += OnTrackStuckAsync;
            this.lavaNode.OnTrackException += OnTrackExceptionAsync;
            this.lavaNode.OnWebSocketClosed += OnWebSocketClosed;
            guildInfo = new ConcurrentDictionary<IGuild, DJGuildInfo>();
        }

        private async Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            Console.WriteLine($"Websocket closed. {arg.Reason}.");
        }

        private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg)
        {
            Console.WriteLine($"{arg.Track} causes exception. {arg.ErrorMessage}.");
        }

        private async Task OnTrackStuckAsync(TrackStuckEventArgs arg)
        {
            Console.WriteLine($"{arg.Track} stuck for {arg.Threshold}.");
        }

        public async Task<string> JoinChannel(IGuild guild, IVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (lavaNode.HasPlayer(guild))
            {
                return await MoveAsync(guild, voiceChannel, textChannel);
            }
            LavaPlayer lp = await lavaNode.JoinAsync(voiceChannel, textChannel);
            if(lp == null)
            {
                return "Error joining voice channel.";
            }
            guildInfo.TryAdd(guild, new DJGuildInfo(voiceChannel));
            return $"Joined {voiceChannel.Name}.";
        }

        public async Task<string> MoveAsync(IGuild guild, IVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (!guildInfo.ContainsKey(guild))
            {
                return "Not currently in a voice channel.";
            }
            await lavaNode.MoveChannelAsync(voiceChannel);
            var player = lavaNode.GetPlayer(guild);
            if(guildInfo.TryGetValue(guild, out DJGuildInfo val))
            {
                val.voiceChannel = voiceChannel;
            }
            return $"Moved from {player.VoiceChannel} to {voiceChannel}.";
        }

        public async Task<string> LeaveChannel(IGuild guild, IVoiceChannel voiceChannel)
        {
            await lavaNode.LeaveAsync(voiceChannel);
            guildInfo.TryRemove(guild, out var value);
            return $"Left {voiceChannel.Name}.";
        }

        public async Task<string> PlayQuery(string username,IGuild guild, IVoiceChannel voiceChannel, 
            ITextChannel textChannel, string query)
        {
            try
            {
                var search = await lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTube, query);

                if (search.Status == Victoria.Responses.Search.SearchStatus.NoMatches)
                {
                    search = await lavaNode.SearchYouTubeAsync(query);

                    if (search.Status == Victoria.Responses.Search.SearchStatus.NoMatches)
                    {
                        search = await lavaNode.SearchSoundCloudAsync(query);

                        if (search.Status == Victoria.Responses.Search.SearchStatus.NoMatches)
                        {
                            return $"Sorry, I couldn't find any matches for {query}.";
                        }
                    }
                }

                if(search.Status == Victoria.Responses.Search.SearchStatus.LoadFailed)
                {
                    return $"Sorry, loading the result failed.";
                }

                LavaTrack track = null;
                if(search.Tracks.FirstOrDefault(x => x.Url.Trim().Contains(query.Trim())) != null)
                {
                    track = search.Tracks.FirstOrDefault(x => x.Url.Trim().Contains(query.Trim()));
                }
                else
                {
                    track = search.Tracks.FirstOrDefault();
                }

                if(track == null)
                {
                    return "Sorry, can't get the track.";
                }

                var player = lavaNode.HasPlayer(guild)
                    ? lavaNode.GetPlayer(guild)
                    : await lavaNode.JoinAsync(voiceChannel, textChannel);


                if (!guildInfo.TryGetValue(guild, out var g))
                {
                    guildInfo.TryAdd(guild, new DJGuildInfo(voiceChannel));
                }

                DJGuildInfo currentGuild = null;
                guildInfo.TryGetValue(guild, out currentGuild);

                currentGuild.Enqueue(username, track);
                if (player.PlayerState == Victoria.Enums.PlayerState.Playing)
                {
                    return $"Enqueued `{track.Title}.`";
                }
                else
                {
                    currentGuild.PlayNext();
                    await player.PlayAsync(currentGuild.currentlyPlaying.track);
                    return $"Playing `{track.Title}`.";
                }
            }catch(Exception e)
            {
                return $"Error playing query: `{e.Message}`.";
            }
        }

        private async Task OnTrackEndedAsync(TrackEndedEventArgs args)
        {
            if (args.Reason != Victoria.Enums.TrackEndReason.Finished)
                return;

            // No users are in the channel, stop playing and leave.
            if((args.Player.VoiceChannel as SocketVoiceChannel).Users.Count == 0)
            {
                await LeaveChannel(args.Player.VoiceChannel.Guild, args.Player.VoiceChannel);
                return;
            }
            
            // Check if we should loop the track that ended.
            DJGuildInfo guild = null;
            if (guildInfo.TryGetValue(args.Player.VoiceChannel.Guild, out guild))
            {
                if (guild.loopSong == true)
                {
                    await args.Player.PlayAsync(args.Track);
                    await args.Player.TextChannel.SendMessageAsync($"Now looping `{args.Track.Title}`.");
                    return;
                }
            }

            // Couldn't find the guild, leave channel.
            if(guild == null)
            {
                await LeaveChannel(args.Player.VoiceChannel.Guild, args.Player.VoiceChannel);
                return;
            }

            guild.OnNextTrack();

            // There was no next track, leave channel.
            if (guild.currentlyPlaying == null)
            {
                await LeaveChannel(args.Player.VoiceChannel.Guild, args.Player.VoiceChannel);
                return;
            }

            LavaTrack track = guild.currentlyPlaying.track as LavaTrack;
            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync($"Now playing `{track.Title}`.");
        }

        public List<DJQueueItem> GetQueue(IGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g))
            {
                return new List<DJQueueItem>();
            }

            return new List<DJQueueItem>(g.queue);
        }

        public bool RemoveFromQueue(IGuild guild, int index)
        {
            if(index < 1)
            {
                return false;
            }

            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g))
            {
                return false;
            }

            g.queue.RemoveAt(index);
            return true;
        }

        public DJQueueItem GetCurrentlyPlaying(SocketGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g))
            {
                return null;
            }

            return g.currentlyPlaying;
        }

        public async Task<string> VoteToSkip(IGuild guild, string username)
        {
            if (!lavaNode.HasPlayer(guild))
            {
                return "Nothing is currently playing.";
            }

            LavaPlayer lPlayer = lavaNode.GetPlayer(guild);
            if(guildInfo.TryGetValue(guild, out var g))
            {
                if(g.currentlyPlaying == null)
                {
                    return "Nothing is currently playing";
                }

                bool userVoted = g.skipVotes.Contains(username.ToLower());

                if (!userVoted)
                {
                    g.skipVotes.Add(username.ToLower());
                }

                int usersInCall = (g.voiceChannel as SocketVoiceChannel).Users.Count-1;
                int minVotes = Math.Max(1, (int)(usersInCall / 2));
                if (g.skipVotes.Contains(g.currentlyPlaying.username))
                {
                    int reachedVotes = g.skipVotes.Count;
                    await lPlayer.SeekAsync(g.currentlyPlaying.track.Duration);
                    return $"Requester {g.currentlyPlaying.username} voted, skipping.";
                }
                else if (g.skipVotes.Count >= minVotes)
                {
                    int reachedVotes = g.skipVotes.Count;
                    await lPlayer.SeekAsync(g.currentlyPlaying.track.Duration);
                    return $"{reachedVotes}/{minVotes} votes reached, skipping.";
                }
                return userVoted ? $"{username} has already voted." : $"{username} voted to skip. {g.skipVotes.Count}/{minVotes}.";
            }
            return "Not able to skip.";
        }

        public async Task<string> SeekTime(IGuild guild, TimeSpan time)
        {
            if (!lavaNode.HasPlayer(guild))
            {
                return "Nothing is currently playing.";
            }

            var player = lavaNode.GetPlayer(guild);

            await player.SeekAsync(time);

            return $"Seeked to {time.ToString()}.";
        }

        public string ShuffleQueue(IGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g))
            {
                return "Nothing is currently playing";
            }

            if(g.queue.Count == 0)
            {
                return "Nothing is in the queue.";
            }

            g.Shuffle();
            return "Shuffled queue.";
        }

        public string ClearQueue(IGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g))
            {
                return "Nothing is currently playing";
            }

            g.queue.Clear();
            return "Emptied queue.";
        }

        public string ToggleLoop(IGuild guild)
        {
            if (!lavaNode.HasPlayer(guild))
            {
                return "Nothing is currently playing.";
            }

            var player = lavaNode.GetPlayer(guild);

            if (guildInfo.TryGetValue(guild, out var g))
            {
                g.loopSong = !g.loopSong;

                return g.loopSong ? $"Looping enabled." : $"Looping disabled.";
            }
            return "Can't toggle looping.";
        }
    }
}
