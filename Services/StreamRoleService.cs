using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TidesBotDotNet.Interfaces;
using TwitchLib.Api;

namespace TidesBotDotNet.Services
{
    public class StreamRoleService
    {
        public class StreamRoleGuildDefinition
        {
            public ulong guildID;
            public ulong roleID;
            // discordID : twitch Username
            public List<(ulong, string)> users = new();

            public StreamRoleGuildDefinition(ulong guildID)
            {
                this.guildID = guildID;
            }

            public void RemoveUser(ulong discordID)
            {
                for(int i = users.Count-1; i >= 0; i--)
                {
                    if (users[i].Item1 != discordID) continue;
                    users.RemoveAt(i);
                }
            }

            public void AddUser(ulong discordID, string twitchUsername)
            {
                var u = users.FirstOrDefault(x => x.Item1 == discordID);
                if (u.Item1 == discordID && u.Item2 == twitchUsername) return; 
                users.Add((discordID, twitchUsername));
            }
        }

        private readonly string streamRoleGuildInfoFilename = "streamroleguildinfo.json";
        private readonly string twitchKeysFilename = "twitchkeys.json";

        private TwitchAPI api;
        private DiscordSocketClient client;
        public GuildsDefinition guildsDefinition;
        private Timer timer;

        public Dictionary<ulong, StreamRoleGuildDefinition> guilds = new();

        public StreamRoleService(DiscordSocketClient client, GuildsDefinition gd)
        {
            this.client = client;
            this.guildsDefinition = gd;
            api = new TwitchAPI();
            TwitchKeys twitchKeys = SaveLoadService.Load<TwitchKeys>(twitchKeysFilename);
            if(twitchKeys == null)
            {
                Console.WriteLine("ERROR: Could not load twitch keys.");
                return;
            }
            api.Settings.ClientId = twitchKeys.clientID;
            api.Settings.Secret = twitchKeys.secret;
            LoadData();
            SaveData();

            // Call the check with the given interval between the calls.
            timer = new Timer(TimeSpan.FromSeconds(300).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler((a, b) => Tick());
            timer.Start();
            Console.WriteLine("Stream role service started.");
        }

        private async void Tick()
        {
            foreach (var guildDef in guilds.Values.ToList())
            {
                var guild = client.GetGuild(guildDef.guildID);
                if (guild == null) continue;
                if (guildDef.roleID == 0) continue;
                var role = guild.GetRole(guildDef.roleID);
                if (role == null) continue;

                foreach(var userDef in guildDef.users.ToList())
                {
                    var guildUser = guild.GetUser(userDef.Item1);
                    if (guildUser == null) continue;

                    bool userLive = await TwitchUserLive(userDef.Item2);
                    if (userLive)
                    {
                        if (guildUser.Roles.Contains(role)) continue;
                        await guildUser.AddRoleAsync(role);
                        continue;
                    }
                    // User not live.
                    if (!guildUser.Roles.Contains(role)) continue;
                    await guildUser.RemoveRoleAsync(role);
                }
            }
        }

        private void LoadData()
        {
            //Load the info for the guilds and the users they want reported.
            string guildInfoResult = SaveLoadService.Load(streamRoleGuildInfoFilename);
            if (guildInfoResult != null)
            {
                guilds = JsonConvert.DeserializeObject<Dictionary<ulong, StreamRoleGuildDefinition>>(guildInfoResult);
            }
        }

        private void SaveData()
        {
            SaveLoadService.Save(streamRoleGuildInfoFilename, JsonConvert.SerializeObject(guilds));
        }

        public bool SetRole(ulong guildID, string roleName)
        {
            if (!guilds.ContainsKey(guildID)) guilds.Add(guildID, new StreamRoleGuildDefinition(guildID));
            var guild = client.GetGuild(guildID);

            var role = guild.Roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower());
            if (role == null) return false;

            guilds[guildID].roleID = role.Id;
            SaveData();
            return true;
        }

        public async Task<string> AddUser(ulong guildID, SocketGuildUser guildUser, string twitchUsername)
        {;
            if (!guilds.ContainsKey(guildID)) guilds.Add(guildID, new StreamRoleGuildDefinition(guildID));
            var g = guilds[guildID];

            if (!(await TwitchUserExist(twitchUsername))) return $"Twitch user {twitchUsername} does not exist.";
            guilds[guildID].AddUser(guildUser.Id, twitchUsername);
            SaveData();
            return null;
        }

        public string RemoveUser(ulong guildID, SocketGuildUser guildUser)
        {
            if (!guilds.ContainsKey(guildID)) return "No users registered in this guild.";
            var g = guilds[guildID];

            var v = g.users.FirstOrDefault(x => x.Item1 == guildUser.Id);
            if (v.Item1 == 0) return "User is not registered.";
            guilds[guildID].RemoveUser(guildUser.Id);
            SaveData();
            return null;
        }

        public async Task<bool> TwitchUserExist(string username)
        {
            var user = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username }, null);
            if (user.Users.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> TwitchUserLive(string username)
        {
            var userList = await api.Helix.Streams.GetStreamsAsync(userLogins: new List<string>() { username });
            if (userList == null || userList.Streams.Count() == 0) return false;
            return true;
        }
    }
}
