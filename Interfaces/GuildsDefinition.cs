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
        public static readonly string REACTROLES_FILENAME = "reactroles.json";

        private Dictionary<ulong, GuildSettings> settings = new Dictionary<ulong, GuildSettings>();
        public Dictionary<ulong, Dictionary<string, List<ReactRolesDefinition>>> reactRoles
            = new Dictionary<ulong, Dictionary<string, List<ReactRolesDefinition>>>();

        public GuildsDefinition()
        {
            settings = SaveLoadService.Load<Dictionary<ulong, GuildSettings>>(FILE_NAME);
            reactRoles = SaveLoadService.Load<Dictionary<ulong, Dictionary<string, List<ReactRolesDefinition>>>>(REACTROLES_FILENAME);
            if(settings == null)
            {
                settings = new Dictionary<ulong, GuildSettings>();
                SaveLoadService.Save(FILE_NAME, settings);
            }
            if (reactRoles == null)
            {
                reactRoles = new Dictionary<ulong, Dictionary<string, List<ReactRolesDefinition>>>();
                SaveLoadService.Save(REACTROLES_FILENAME, reactRoles);
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

        public void SaveReactRoles()
        {
            SaveLoadService.Save(REACTROLES_FILENAME, reactRoles, Formatting.Indented);
        }
    }
}
