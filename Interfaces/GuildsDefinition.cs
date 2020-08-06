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
            settings = SaveLoadService.Load<Dictionary<ulong, GuildSettings>>(FILE_NAME);
            if(settings == null)
            {
                settings = new Dictionary<ulong, GuildSettings>();
                SaveLoadService.Save(FILE_NAME, settings);
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
            SaveLoadService.Save(FILE_NAME, settings);
        }
    }
}
