using Discord.Interactions;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;

namespace TidesBotDotNet.Modules
{
    [Group("settings", "General guild settings.")]
    public class SettingsModule : InteractionModuleBase<SocketInteractionContext>
    {
        public GuildsDefinition gd;

        [Group("user", "Settings related to users.")]
        public class SettingsUserModule : InteractionModuleBase<SocketInteractionContext>
        {
            public GuildsDefinition gd;

            public SettingsUserModule(GuildsDefinition guildsDefinition)
            {
                gd = guildsDefinition;
            }

            [SlashCommand("colorme-status", "Check colorme permission.")]
            public async Task ColorMe()
            {
                await RespondAsync($"Colorme is currently {(gd.GetSettings(Context.Guild.Id).colorMe ? "enabled" : "disabled")}.", ephemeral: true);
            }

            [SlashCommand("colorme", "Set colorme enabled state.")]
            public async Task ColorMe(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).colorMe = enabled;
                gd.SaveSettings();
                await RespondAsync($"ColorMe is now " + ( enabled ? "enabled" : "disabled" ) + " in this guild.", ephemeral: true);
            }
        }
    }
}
