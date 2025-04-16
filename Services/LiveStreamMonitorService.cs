using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TidesBotDotNet.Interfaces;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Interfaces;

namespace TidesBotDotNet.Services
{
    public class OnStreamArgs
    {
        public string Username { get; private set; } = "";
        public Stream Stream { get; private set; } = null;
        public OnStreamArgs(string username, Stream stream)
        {
            this.Username = username;
            this.Stream = stream;
        }
    }

    public class LiveStreamMonitorService
    {
        private readonly string monitoredUsersFilename = "twitchmonitor.json";

        /// <summary>
        /// A cache with streams that are currently live. The key is the username.
        /// </summary>
        public ConcurrentDictionary<string, Stream> LiveStreams { get; set; } 
            = new ConcurrentDictionary<string, Stream>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Invoked when a monitored stream went online.
        /// </summary>
        public event EventHandler<OnStreamArgs> OnStreamOnline;
        /// <summary>
        /// Invoked when a monitored stream went offline.
        /// </summary>
        public event EventHandler<OnStreamArgs> OnStreamOffline;
        /// <summary>
        /// Invoked when a monitored stream was already online, but is updated with it's latest information (might be the same).
        /// </summary>
        public event EventHandler<OnStreamArgs> OnStreamUpdate;

        public event EventHandler OnTrackedUserUpdateSuccessful;
        public event EventHandler OnTrackedUserUpdateUnsuccessful;

        private ITwitchAPI api;
        private Timer timer;

        private HashSet<string> usersBeingTracked = new HashSet<string>();

        private bool ticking = false;

        public LiveStreamMonitorService(ITwitchAPI api, int secondsBetweenChecks)
        {
            timer = new Timer(TimeSpan.FromSeconds(secondsBetweenChecks).TotalMilliseconds);

            this.api = api;

            //Load the info for streams that have been reported on.
            //string monitoredUsersResult = SaveLoadService.Load(monitoredUsersFilename);
            Dictionary<string, Stream> monitoredChannels = SaveLoadService.Load<Dictionary<string, Stream>>(monitoredUsersFilename);
            if(monitoredChannels == null)
            {
                monitoredChannels = new Dictionary<string, Stream>();
                SaveLoadService.Save(monitoredUsersFilename, monitoredChannels);
            }

            LiveStreams.Clear();
            foreach (var key in monitoredChannels.Keys)
            {
                LiveStreams.TryAdd(key, monitoredChannels[key]);
            }
        }

        public void StartTimer(int secondsBetweenChecks)
        {
            // Call the check with the given interval between the calls.
            timer.Interval = TimeSpan.FromSeconds(secondsBetweenChecks).TotalMilliseconds;
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(Tick);
            timer.Start();
        }

        /// <summary>
        /// Adds users to the monitoring list.
        /// </summary>
        /// <param name="usernames">The username(s) of the user(s) you want to add.</param>
        /// <returns>A list of the successfully added users.</returns>
        public async Task<List<string>> AddTrackedUsers(params string[] usernames)
        {
            if (usernames == null || usernames.Length == 0)
            {
                return new List<string>();
            }
            List<string> addedUsers = new List<string>();
            try
            {
                var users = await api.Helix.Users.GetUsersAsync(null, usernames.ToList());
                if (users != null)
                {
                    for (int i = 0; i < users.Users.Count(); i++)
                    {
                        if (string.IsNullOrWhiteSpace(users.Users[i].Login))
                        {
                            Logger.WriteLine($"WARNING: Found user with null or empty login name. Assumed index of {i}. List: {string.Join(",", usernames.ToList())}");
                            continue;
                        }
                        if (usersBeingTracked.Add(users.Users[i].Login))
                        {
                            addedUsers.Add(users.Users[i].Login);
                        }
                    }
                }
                OnTrackedUserUpdateSuccessful?.Invoke(this, null);
                return addedUsers;
            }
            catch (Exception e)
            {
                Logger.WriteLine($"ERROR: Exception thrown while adding users to the monitor. {e}");
                //OnTrackedUserUpdateUnsuccessful?.Invoke(this, null);
                return null;
            }
        }

        /// <summary>
        /// Removes users from the monitoring list.
        /// </summary>
        /// <param name="usernames">The user(s) you want removed.</param>
        /// <returns>A list of the successfully removed users.</returns>
        public List<string> RemoveTrackedUsers(params string[] usernames)
        {
            List<string> removedUsers = new List<string>();
            for(int i = 0; i < usernames.Count(); i++)
            {
                if (usersBeingTracked.Remove(usernames[i]))
                {
                    if (LiveStreams.TryRemove(usernames[i], out Stream v))
                    {
                        removedUsers.Add(usernames[i]);
                    }
                }
            }
            usersBeingTracked.RemoveWhere(x => string.IsNullOrWhiteSpace(x));
            return removedUsers;
        }

        public void ClearTrackedUsers()
        {
            usersBeingTracked.Clear();
        }

        /// <summary>
        /// Checks which users are live, along with who has gone offline.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Tick(object sender, ElapsedEventArgs e)
        {
            if(ticking == true)
            {
                return;
            }
            ticking = true;

            try
            {
                List<string> liveUsers = new List<string>();
                var s = await GetStreams();
                if (s != null)
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        // User is/was live.
                        if (LiveStreams.ContainsKey(s[i].UserName))
                        {
                            // It's the same stream.
                            if (LiveStreams[s[i].UserName].Id == s[i].Id)
                            {
                                LiveStreams[s[i].UserName] = s[i];
                                // Invoke event.
                                OnStreamArgs onStreamUpdateArgs = new OnStreamArgs(s[i].UserName, s[i]);
                                OnStreamUpdate?.Invoke(this, onStreamUpdateArgs);
                            }
                            // Different stream and the time between the streams is at least 2 hours apart.
                            else if ((s[i].StartedAt - LiveStreams[s[i].UserName].StartedAt).TotalHours > 2.0)
                            {
                                LiveStreams.TryRemove(s[i].UserName, out Stream v);

                                if (LiveStreams.TryAdd(s[i].UserName, s[i]))
                                {
                                    Logger.WriteLine($"User {s[i].UserName} was live, but started a new stream after 2 hours.");
                                    // Invoke event.
                                    OnStreamArgs onStreamOnlineArgs = new OnStreamArgs(s[i].UserName, s[i]);
                                    OnStreamOnline?.Invoke(this, onStreamOnlineArgs);
                                }
                            }
                            else
                            {
                                LiveStreams.Remove(s[i].UserName, out var v);
                                LiveStreams.TryAdd(s[i].UserName, s[i]);
                            }
                        }
                        // User was not live before.
                        else
                        {
                            Logger.WriteLine($"User {s[i].UserName} was not live before.");
                            if (LiveStreams.TryAdd(s[i].UserName, s[i]))
                            {
                                // Invoke event.
                                OnStreamArgs oso = new OnStreamArgs(s[i].UserName, s[i]);
                                OnStreamOnline?.Invoke(this, oso);
                            }
                            else
                            {
                                Logger.WriteLine($"ERROR: adding user {s[i].UserName} to livestreams list.");
                            }
                        }

                        liveUsers.Add(s[i].UserName);
                    }

                    //Cleanup(liveUsers);
                }
            }catch(Exception ex)
            {
                Logger.WriteLine($"ERROR: error during LiveStreamMonitorService tick: {ex}");
                //api.Settings.AccessToken = String.Empty;
                //api.Settings.AccessToken = await (api as TwitchAPI).Auth.GetAccessTokenAsync();
            }
            ticking = false;

            SaveLoadService.Save(monitoredUsersFilename, LiveStreams);
        }

        /// <summary>
        /// Cleanup streams that have gone offline.
        /// </summary>
        /// <param name="users"></param>
        private void Cleanup(List<string> users)
        {
            List<string> streamsToRemove = new List<string>();
            foreach(string k in LiveStreams.Keys)
            {
                if (!users.Contains(k))
                {
                    streamsToRemove.Add(k);
                }
            }
            foreach(string s in streamsToRemove)
            {
                OnStreamArgs offlineArgs = new OnStreamArgs(s, LiveStreams[s]);
                OnStreamOffline?.Invoke(this, offlineArgs);
                LiveStreams.TryRemove(s, out Stream v);
            }
        }

        /// <summary>
        /// Gets all the currently live streams of the users we're tracking.
        /// </summary>
        /// <returns>A list of all the live streams.</returns>
        public async Task<Stream[]> GetStreams()
        {
            if(usersBeingTracked == null)
            {
                return null;
            }
            if(usersBeingTracked.Count() == 0)
            {
                return null;
            }

            usersBeingTracked.RemoveWhere(x => string.IsNullOrWhiteSpace(x));

            try
            {
                GetStreamsResponse liveStreams = await api.Helix.Streams.GetStreamsAsync(first: 100, userLogins: usersBeingTracked.ToList());
                return liveStreams.Streams;
            }catch(Exception e)
            {
                Logger.WriteLine($"Exception thrown while fetching streams. {e}.\nList is {string.Join(",", usersBeingTracked)}");
                return null;
            }
        }
    }
}
