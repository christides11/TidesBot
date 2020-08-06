using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Interfaces
{
    public class GuildsDefinition
    {
        public static readonly string FILE_NAME = "guildsdefinition.json";

        private Dictionary<ulong, GuildSettings> settings = new Dictionary<ulong, GuildSettings>();

        public GuildsDefinition()
        {
            string f = SaveLoadService.Load(FILE_NAME);
            if(f != null)
            {
                settings = JsonConvert.DeserializeObject<Dictionary<ulong, GuildSettings>>(f);
            }
        }

        public GuildSettings GetSettings(ulong id)
        {
            if (!settings.ContainsKey(id))
            {
                settings.Add(id, new GuildSettings());
            }
            return settings[id];
        }

        public void SaveSettings()
        {
            SaveLoadService.Save(FILE_NAME, JsonConvert.SerializeObject(settings));
        }
    }
}
