﻿using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace TidesBotDotNet.Services
{
    public class TwitchService
    {
        public class TwitchGuildDefinition
        {
            public ulong guildID;
            public ulong textChannelID;
            public int previewMode = 1;
            public bool adminOnly = true;
            public List<string> users = new List<string>();
            public Dictionary<string, int> previewModeOverrides = new Dictionary<string, int>();

            public TwitchGuildDefinition(ulong guildID, ulong textChannelID)
            {
                this.guildID = guildID;
                this.textChannelID = textChannelID;
            }
        }

        private readonly string twitchGuildInfoFilename = "twitchguildinfo.json";
        private readonly string twitchKeysFilename = "twitchkeys.json";
        private readonly bool serviceEnabled = true;
        
        private TwitchAPI api;
        private LiveStreamMonitorService monitorService;
        private DiscordSocketClient client;

        public List<TwitchGuildDefinition> guilds = new List<TwitchGuildDefinition>();

        //Function to get random number
        private static readonly Random getrandom = new Random();

        public TwitchService(DiscordSocketClient client)
        {
            if (!serviceEnabled)
            {
                Logger.WriteLine("Twitch service disabled.");
                return;
            }
            this.client = client;
            _ = Setup();
        }

        private async Task Setup()
        {
            Logger.WriteLine("Twitch service starting up.");
            api = new TwitchAPI();
            TwitchKeys twitchKeys = SaveLoadService.Load<TwitchKeys>(twitchKeysFilename);
            if (twitchKeys == null)
            {
                twitchKeys = new TwitchKeys();
                SaveLoadService.Save(twitchKeysFilename, twitchKeys);
            }

            api.Settings.ClientId = twitchKeys.clientID;
            api.Settings.Secret = twitchKeys.secret;
            api.Settings.SkipAutoServerTokenGeneration = false;
            var gotAccessToken = await api.Auth.GetAccessTokenAsync();

            if(gotAccessToken == null)
            {
                api.Settings.AccessToken = twitchKeys.accessToken;
            }
            else
            {
                api.Settings.AccessToken = gotAccessToken;
                Logger.WriteLine($"Got new access token: {twitchKeys.accessToken}");
            }

            monitorService = new LiveStreamMonitorService(api, 120);

            LoadData();

            UpdateMonitoredChannels();

            await _AttemptUpdateTrackedUsers();
            monitorService.StartTimer(120);
            
            monitorService.OnStreamOnline += OnStreamOnline;
            monitorService.OnStreamOffline += OnStreamOffline;
            monitorService.OnTrackedUserUpdateUnsuccessful += AttemptUpdateTrackedUsersAgain;

            Logger.WriteLine("Twitch service successfully initialized.");
        }

        private void LoadData()
        {
            //Load the info for the guilds and the users they want reported.
            string guildInfoResult = SaveLoadService.Load(twitchGuildInfoFilename);
            if (guildInfoResult != null)
            {
                guilds = JsonConvert.DeserializeObject<List<TwitchGuildDefinition>>(guildInfoResult);
            }
        }

        #region Module
        public void SetAlertChannel(ulong guildID, ulong channelID)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if (guildDefinition == null)
            {
                guilds.Add(new TwitchGuildDefinition(guildID, channelID));
                guildDefinition = guilds[guilds.Count - 1];
            }

            guildDefinition.textChannelID = channelID;

            UpdateMonitoredChannels();
        }

        public async Task<string> AddUser(ulong guildID, ulong channelID, string username)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if (guildDefinition == null)
            {
                guilds.Add(new TwitchGuildDefinition(guildID, channelID));
                guildDefinition = guilds[guilds.Count-1];
            }

            if(!await UserExist(username))
            {
                return "User does not exist.";
            }

            if(guildDefinition.users.Contains(username))
            {
                return "User is already being reported for this guild.";
            }

            guildDefinition.users.Add(username);

            UpdateMonitoredChannels();
            return $"Added https://www.twitch.tv/{username} to list.";
        }

        public string RemoveUser(ulong guildID, string username)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if(guildDefinition == null)
            {
                return "There are no users reporting in this guild.";
            }

            if (!guildDefinition.users.Contains(username))
            {
                return "User is not in the list.";
            }

            guildDefinition.users.Remove(username);
            monitorService.LiveStreams.TryRemove(username, out Stream s);

            UpdateMonitoredChannels();
            return $"Removed {username} from list.";
        }
        #endregion

        public async Task<bool> UserExist(string username)
        {
            var user = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username }, null);
            if (user.Users.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public string[] GetUsersInGuild(ulong guildID)
        {
            string[] users = new string[0];

            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if (guildDefinition != null)
            {
                users = guildDefinition.users.ToArray();
            }
            return users;
        }

        public TwitchGuildDefinition GetGuild(ulong guildID)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);
            return guildDefinition;
        }

        public bool SetPreviewMode(ulong guildID, int previewMode)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if (guildDefinition == null)
            {
                return false;
            }

            guildDefinition.previewMode = previewMode;
            UpdateMonitoredChannels();
            return true;
        }

        public bool SetUserPreviewMode(ulong guildID, string username, int previewMode)
        {
            TwitchGuildDefinition guildDefinition = guilds.Find(x => x.guildID == guildID);

            if (guildDefinition == null)
            {
                return false;
            }

            if (guildDefinition.previewModeOverrides.ContainsKey(username.ToLower()))
            {
                guildDefinition.previewModeOverrides[username.ToLower()] = previewMode;
            }
            else
            {
                guildDefinition.previewModeOverrides.Add(username.ToLower(), previewMode);
            }

            UpdateMonitoredChannels();
            return true;
        }

        public async Task<bool> CheckIfOnline(string username)
        {
            var userList = await api.Helix.Streams.GetStreamsAsync( userLogins: new List<string>() { username });

            if (userList != null)
            {
                if(userList.Streams.Count() > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #region Monitoring
        private void UpdateMonitoredChannels()
        {
            // Get all channels we should report on.
            List<string> users = new List<string>();
            foreach(var guild in guilds)
            {
                users.AddRange(guild.users);
            }

            _ = monitorService.AddTrackedUsers(users.Distinct().ToArray());

            SaveLoadService.Save(twitchGuildInfoFilename, guilds);
        }


        bool currentlyTryingAgain = false;
        private async void AttemptUpdateTrackedUsersAgain(object sender, EventArgs e)
        {
            if (currentlyTryingAgain) return;
            currentlyTryingAgain = true;

            await _AttemptUpdateTrackedUsers();

            currentlyTryingAgain = false;
        }

        private async Task _AttemptUpdateTrackedUsers()
        {
            List<string> users = new List<string>();
            foreach (var guild in guilds)
            {
                users.AddRange(guild.users);
            }

            await _AttemptUpdateTrackedUsersLoop(users, 0);
        }

        private async Task _AttemptUpdateTrackedUsersLoop(List<string> users, int depth)
        {
            if(depth > 0) Logger.WriteLine($"Attempting to update tracked users again. Attempt {depth}");
            var r = await monitorService.AddTrackedUsers(users.Distinct().ToArray());

            if (depth == 10) throw new Exception("Could not update monitored channels after 10 attempts.");
            if (r == null)
            {
                await Task.Delay(10000);
                await _AttemptUpdateTrackedUsersLoop(users, depth+1);
            }
        }

        private async void OnStreamOnline(object sender, OnStreamArgs e)
        {
            try
            {
                foreach (TwitchGuildDefinition guild in guilds)
                {
                    if (client.GetGuild(guild.guildID) == null) continue; 
                    //If the user should be reported on in this guild.
                    if (!String.IsNullOrEmpty(guild.users.Find(x => x.ToLower() == e.Stream.UserName.ToLower())))
                    {
                        var textChannel = client.GetGuild(guild.guildID).TextChannels.FirstOrDefault(x => x.Id == guild.textChannelID);

                        if (textChannel == null)
                        {
                            Logger.WriteLine($"Text channel {guild.textChannelID} in {guild.guildID} does not exist.");
                            continue;
                        }

                        //var uc = await api.Helix.Users.GetUsersFollowsAsync(null, null, 1, null, e.Stream.UserId);
                        var sGame = (await api.Helix.Games.GetGamesAsync(new List<string>() { e.Stream.GameId }));
                        var streamGame = sGame.Games.Count() > 0 ? sGame.Games[0] : null;
                        var streamGameName = streamGame == null ? "?" : streamGame.Name;

                        TimeSpan timeLive = (DateTime.UtcNow - e.Stream.StartedAt);
                        int hoursUp = ((int)timeLive.TotalMinutes) / 60;
                        int minutesUp = (int)timeLive.TotalMinutes - (60 * hoursUp);
                        string timeLiveString = timeLive.TotalMinutes < 60 ? $"{(int)timeLive.TotalMinutes} minutes."
                            : $"{hoursUp} hours, {minutesUp} minutes.";

                        var output = new EmbedBuilder()
                            .WithTitle($"{e.Stream.UserName} is online!")
                            .AddField($"Playing {streamGameName}", $"[{e.Stream.Title}](https://www.twitch.tv/{e.Stream.UserName})")
                            .AddField($"Current Viewers:", $"{e.Stream.ViewerCount}")
                            .AddField("Up For:", timeLiveString);

                        /*
                        output.Footer = new EmbedFooterBuilder
                        {
                            Text = $"{uc.TotalFollows} followers."
                        };*/

                        int userPreviewMode = guild.previewMode;

                        if (guild.previewModeOverrides.ContainsKey(e.Stream.UserName.ToLower()))
                        {
                            userPreviewMode = guild.previewModeOverrides[e.Stream.UserName.ToLower()];
                        }

                        switch (userPreviewMode)
                        {
                            case 1:
                                output.WithImageUrl($"{e.Stream.ThumbnailUrl.Replace("{width}", "1280").Replace("{height}", "720")}?{e.Stream.UserName}.{e.Stream.ViewerCount}.{timeLive.TotalMinutes}.{getrandom.Next(0, int.MaxValue)}");
                                if (streamGame != null)
                                {
                                    string boxArtURL = streamGame.BoxArtUrl.Replace("{width}", "425").Replace("{height}", "550");
                                    try
                                    {
                                        output.WithThumbnailUrl(boxArtURL);
                                    }
                                    catch (Exception exception)
                                    {
                                        Logger.WriteLine($"Twitch error with url: {boxArtURL}. {exception}");
                                    }
                                }
                                break;
                            case 2:
                                if (streamGame != null)
                                {
                                    string boxArtURL = streamGame.BoxArtUrl.Replace("{width}", "425").Replace("{height}", "550");
                                    try
                                    {
                                        output.WithThumbnailUrl(boxArtURL);
                                    }
                                    catch (Exception exception)
                                    {
                                        Logger.WriteLine($"Twitch error with url: {boxArtURL}. {exception}");
                                    }
                                }
                                break;
                        }


                        await textChannel.SendMessageAsync("", false, output.Build());
                    }
                }
            }catch(Exception exc)
            {
                Logger.WriteLine($"Exception thrown while reporting online stream. {exc}");
            }
        }

        private void OnStreamOffline(object sender, OnStreamArgs e)
        {

        }
        #endregion
    }
}
