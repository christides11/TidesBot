using Discord;
using Discord.Audio;
using Discord.Rest;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Services
{
    public class DJService
    {
        private IAudioService _audioService;
        
        public DJService(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public async Task<(string, Exception)> JoinChannel(IGuild guild, IVoiceChannel voiceChannel)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id) ?? await _audioService.JoinAsync<DJPlayer>(guild.Id, voiceChannel.Id);

                return ($"Joined {voiceChannel.Name}", null);
            }catch(Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> LeaveChannel(IGuild guild, IVoiceChannel voiceChannel)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);

                if (player != null) await player.DisconnectAsync(); 

                return ($"Left voice channel.", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> PlayQuery(SocketUser username, IGuild guild, IVoiceChannel voiceChannel,
            string query, SearchMode searchMode)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id) ?? await _audioService.JoinAsync<DJPlayer>(guild.Id, voiceChannel.Id);

                bool hasQueue = player.CurrentTrack != null;

                var response = await _audioService.GetTracksAsync(query, mode: searchMode);

                if(response.Count() == 0)
                {
                    return ("No tracks found.", null);
                }

                foreach (var t in response)
                {
                    await player.PlayAsync(t);
                }

                return (hasQueue ? (response.Count() == 1 ? $"**Enqueued** {response.First().Title}" : $"**Enqueued** {response.Count()} songs.") : $"**Playing** 🎶 `{response.First().Title}`", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }

        public LavalinkQueue GetQueue(IGuild guild)
        {
            var player = _audioService.GetPlayer<DJPlayer>(guild.Id);

            if (player == null) return default;
            return player.Queue;
        }

        public bool RemoveFromQueue(IGuild guild, int index)
        {
            if (index < 1) return false;
            var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
            if(player == null || player.Queue.Count == 0) return false;
            player.Queue.RemoveAt(index);
            return true;
        }

        public LavalinkTrack GetCurrentlyPlaying(IGuild guild)
        {
            var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
            if (player == null) return null;
            return player.CurrentTrack;
        }

        public TrackPosition GetCurrentPosition(IGuild guild)
        {
            var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
            if (player == null || player.CurrentTrack == null) return default(TrackPosition);
            return player.Position;
        }

        public async Task<(string, Exception)> VoteToSkip(IGuild guild, IUser user)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
                if (player == null || player.CurrentTrack == null) throw new Exception("DJ is not currently playing anything.");
                var r = await player.GetVoteInfoAsync();
                if (r.Votes.Contains(user.Id)) return ($"{user.Username} has already voted.", null);
                await player.VoteAsync(user.Id);
                return ($"{user.Username} voted to skip. {r.Percentage} voted.", null);
            }catch(Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> SeekTime(IGuild guild, TimeSpan time)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
                if (player == null || player.CurrentTrack == null) throw new Exception("DJ is not currently playing anything.");
                await player.SeekPositionAsync(time);
                return ($"Seeked to {time.ToString()}.", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> ShuffleQueue(IGuild guild)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
                if (player == null || player.Queue.Count == 0) throw new Exception("There is nothing in the queue.");
                player.Queue.Shuffle();
                return ($"Shuffled queue.", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> ClearQueue(IGuild guild)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
                if (player == null || player.Queue.Count == 0) throw new Exception("There is nothing in the queue.");
                player.Queue.Clear();
                return ($"Cleared queue.", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }

        public async Task<(string, Exception)> ToggleLoop(IGuild guild)
        {
            try
            {
                var player = _audioService.GetPlayer<DJPlayer>(guild.Id);
                if (player == null || player.CurrentTrack == null) throw new Exception("There is nothing currently playing.");
                player.IsLooping = !player.IsLooping;
                return (player.IsLooping ? $"Looping enabled." : "Looping disabled.", null);
            }
            catch (Exception e)
            {
                return ("", e);
            }
        }
    }
}
