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
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

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

            public void Enqueue(SocketUser user, LavaTrack track)
            {
                queue.Add(new DJQueueItem(user, track));
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
            public SocketUser user;
            public LavaTrack track;

            public DJQueueItem()
            {

            }

            public DJQueueItem(SocketUser user, LavaTrack track)
            {
                this.user = user;
                this.track = track;
            }
        }

        //private LavaNode lavaNode;

        private LavaNode<LavaPlayer, LavaTrack> lavaNode;

        private ConcurrentDictionary<IGuild, DJGuildInfo> guildInfo = new ConcurrentDictionary<IGuild, DJGuildInfo>();

        public DJService(LavaNode<LavaPlayer, LavaTrack> lavaNode)
        {
            this.lavaNode = lavaNode;
            //lavaNode.OnTrackEnd += OnTrackEndedAsync;
            lavaNode.OnTrackStuck += OnTrackStuckAsync;
            //lavaNode.OnTrackException += OnTrackExceptionAsync;
            lavaNode.OnWebSocketClosed += OnWebSocketClosed;
            lavaNode.OnUpdateReceived += OnUpdateReceived;
            guildInfo = new ConcurrentDictionary<IGuild, DJGuildInfo>();
        }

        private async Task OnUpdateReceived(UpdateEventArg<LavaPlayer, LavaTrack> arg)
        {
            Console.WriteLine($"UPDATE: {arg.Position}");
        }

        private async Task OnWebSocketClosed(WebSocketClosedEventArg arg)
        {
            Console.WriteLine($"Websocket closed: {arg.Reason}.");
        }

        private async Task OnTrackExceptionAsync(TrackExceptionEventArg<LavaPlayer, LavaTrack> arg)
        {
            Console.WriteLine($"{arg.Track} causes exception: {arg.Exception}.");
            await LeaveChannel(arg.Player.VoiceChannel.Guild, arg.Player.VoiceChannel);
        }

        private async Task OnTrackStuckAsync(TrackStuckEventArg<LavaPlayer, LavaTrack> arg)
        {
            Console.WriteLine($"{arg.Track} stuck for {arg.Threshold}.");
            await LeaveChannel(arg.Player.VoiceChannel.Guild, arg.Player.VoiceChannel);
        }

        private async Task OnTrackEndedAsync(TrackEndReason Reason, LavaPlayer<LavaTrack> Player, LavaTrack Track)
        {
            //Console.WriteLine("TRACK ENDED");
            //if (Reason != TrackEndReason.Finished || Reason != TrackEndReason.Stopped) return;
            if ((Player.VoiceChannel as SocketVoiceChannel).Users.Count == 0) await LeaveChannel(Player.VoiceChannel.Guild, Player.VoiceChannel);

            // Check if we should loop the track that ended.
            DJGuildInfo guild = null;
            if (guildInfo.TryGetValue(Player.VoiceChannel.Guild, out guild))
            {
                if (guild.loopSong == true)
                {
                    await Player.PlayAsync(Track);
                    await Player.TextChannel.SendMessageAsync($"**Looping** 🎶 `{Track.Title}`");
                    return;
                }
            }

            // Couldn't find the guild, leave channel.
            if (guild == null)
            {
                await LeaveChannel(Player.VoiceChannel.Guild, Player.VoiceChannel);
                return;
            }

            guild.OnNextTrack();

            // There was no next track, leave channel.
            if (guild.currentlyPlaying == null)
            {
                await LeaveChannel(Player.VoiceChannel.Guild, Player.VoiceChannel);
                return;
            }
            //Console.WriteLine("2");

            LavaTrack track = guild.currentlyPlaying.track as LavaTrack;
            await Player.PlayAsync(track);
            await Player.TextChannel.SendMessageAsync($"**Playing** 🎶`{track.Title}`");
        }

        public async Task<string> JoinChannel(IGuild guild, IVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (lavaNode.HasPlayer(guild)) return await MoveAsync(guild, voiceChannel, textChannel);

            var lp = await lavaNode.JoinAsync(voiceChannel, textChannel);
            if(lp == null)
            {
                return "Error joining voice channel.";
            }
            guildInfo.TryAdd(guild, new DJGuildInfo(voiceChannel));
            return $"Joined {voiceChannel.Name}.";
        }

        public async Task<string> MoveAsync(IGuild guild, IVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (!guildInfo.ContainsKey(guild)) return "Not currently in a voice channel.";

            await lavaNode.LeaveAsync(voiceChannel);
            var lp = await lavaNode.JoinAsync(voiceChannel);
            if(guildInfo.TryGetValue(guild, out DJGuildInfo val))
            {
                val.voiceChannel = voiceChannel;
            }
            return $"Moved to {voiceChannel}.";
        }

        public async Task<string> LeaveChannel(IGuild guild, IVoiceChannel voiceChannel)
        {
            await lavaNode.LeaveAsync(voiceChannel);
            guildInfo.TryRemove(guild, out var value);
            return $"Left {voiceChannel.Name}.";
        }

        public async Task<string> PlayQuery(SocketUser username, IGuild guild, IVoiceChannel voiceChannel, 
            ITextChannel textChannel, string query)
        {
            try
            {
                var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) 
                    ? await lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.Direct, query)
                    : await lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTube, query);

                if(search.Status == Victoria.Responses.Search.SearchStatus.LoadFailed) return $"Sorry, failed to load query.";

                LavaTrack track = null;
                track = search.Tracks.FirstOrDefault();

                if (track == null) return "Sorry, can't find the track.";

                if (lavaNode.TryGetPlayer(guild, out var player) == false) player = await lavaNode.JoinAsync(voiceChannel, textChannel);
                if (!guildInfo.TryGetValue(guild, out var g)) guildInfo.TryAdd(guild, new DJGuildInfo(voiceChannel));

                DJGuildInfo currentGuild = null;
                guildInfo.TryGetValue(guild, out currentGuild);

                currentGuild.Enqueue(username, track);
                if (player.PlayerState == PlayerState.Playing) return $"**Enqueued** `{track.Title}`";
                
                currentGuild.PlayNext();
                await player.PlayAsync(currentGuild.currentlyPlaying.track);
                return $"**Playing** 🎶 `{track.Title}`";
            }catch(Exception e)
            {
                return $"Error playing query: `{e.Message}`.";
            }
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
            if(index < 1) return false;
            if (!guildInfo.TryGetValue(guild, out DJGuildInfo g)) return false;
            g.queue.RemoveAt(index);
            return true;
        }

        public DJQueueItem GetCurrentlyPlaying(SocketGuild guild)
        {
            if (!lavaNode.TryGetPlayer(guild, out var player)) return null;
            if (!guildInfo.TryGetValue(guild, out var g)) return null;

            return new DJQueueItem { user = g.currentlyPlaying.user, track = player.Track };
        }

        public async Task<string> VoteToSkip(IGuild guild, string username)
        {
            if (!lavaNode.TryGetPlayer(guild, out var lPlayer))
            {
                return "Nothing is currently playing.";
            }

            if (!guildInfo.TryGetValue(guild, out var g))
            {
                return "Not able to skip.";
            }
            if(g.currentlyPlaying == null)
            {
                return "Nothing is currently playing";
            }

            bool userVoted = g.skipVotes.Contains(username.ToLower());
            if (!userVoted) g.skipVotes.Add(username.ToLower());

            int usersInCall = (g.voiceChannel as SocketVoiceChannel).Users.Count-1;
            int minVotes = Math.Max(1, (int)(usersInCall / 2));
            if (g.skipVotes.Contains(g.currentlyPlaying.user.Username.ToLower()))
            {
                int reachedVotes = g.skipVotes.Count;
                await lPlayer.SeekAsync(lPlayer.Track.Duration);//g.currentlyPlaying.track.Duration);
                _ = OnTrackEndedAsync(TrackEndReason.Finished, lPlayer, lPlayer.Track);
                return $"Requester {g.currentlyPlaying.user} voted, skipping.";
            }
            else if (g.skipVotes.Count >= minVotes)
            {
                int reachedVotes = g.skipVotes.Count;
                await lPlayer.SeekAsync(lPlayer.Track.Duration);//g.currentlyPlaying.track.Duration);
                string resultText = $"{reachedVotes}/{minVotes} votes reached, skipping.";
                _ = OnTrackEndedAsync(TrackEndReason.Finished, lPlayer, lPlayer.Track);
                return resultText;
            }
            return userVoted ? $"{username} has already voted." : $"{username} voted to skip. {g.skipVotes.Count}/{minVotes}.";
        }

        public async Task<string> SeekTime(IGuild guild, TimeSpan time)
        {
            if (!lavaNode.TryGetPlayer(guild, out var player)) return "Nothing is currently playing.";

            await player.SeekAsync(time);
            return $"Seeked to {time.ToString()}.";
        }

        public string ShuffleQueue(IGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g)) return "Nothing is currently playing";
            if (g.queue.Count == 0) return "Nothing is in the queue.";

            g.Shuffle();
            return "Shuffled queue.";
        }

        public string ClearQueue(IGuild guild)
        {
            DJGuildInfo g = null;
            if (!guildInfo.TryGetValue(guild, out g)) return "Nothing is currently playing";

            g.queue.Clear();
            return "Emptied queue.";
        }

        public string ToggleLoop(IGuild guild)
        {
            if (!lavaNode.TryGetPlayer(guild, out var player)) return "Nothing is currently playing.";

            if (guildInfo.TryGetValue(guild, out var g))
            {
                g.loopSong = !g.loopSong;
                return g.loopSong ? $"Looping enabled." : $"Looping disabled.";
            }
            return "Can't toggle looping.";
        }
    }
}
