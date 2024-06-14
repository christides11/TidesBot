using Discord.Interactions;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;

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
            public VxTwitterService vxt;

            public SettingsUserModule(GuildsDefinition guildsDefinition, VxTwitterService vxtwit)
            {
                gd = guildsDefinition;
                vxt = vxtwit;
            }

            [SlashCommand("colorme-status", "Check colorme permission.")]
            public async Task ColorMe()
            {
                await RespondAsync($"Colorme is currently {(gd.GetSettings(Context.Guild.Id).colorMe ? "enabled" : "disabled")}.", ephemeral: true);
            }

            [SlashCommand("colorme", "Set colorme enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task ColorMe(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).colorMe = enabled;
                gd.SaveSettings();
                await RespondAsync($"ColorMe is now " + ( enabled ? "enabled" : "disabled" ) + " in this guild.", ephemeral: true);
            }

            [SlashCommand("vxtwitter", "Set vxtwitter enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task VxTwitter(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).vxTwitter = enabled;
                gd.SaveSettings();
                await RespondAsync($"VxTwitter is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("fxtwitter", "Set fxtwitter enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task FxTwitter(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).fxTwitter = enabled;
                gd.SaveSettings();
                await RespondAsync($"FxTwitter is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("vxtiktok", "Set vxtiktok (web link) enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task VxTiktok(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).vxTiktok = enabled;
                gd.SaveSettings();
                await RespondAsync($"VxTiktok is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("vxstiktok", "Set vxtiktok (mobile link) enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task VxSTiktok(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).vxShortTiktok = enabled;
                gd.SaveSettings();
                await RespondAsync($"VxSTiktok is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("vxinstagram", "Set vxinstagram enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task VxInstagram(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).vxInstagram = enabled;
                gd.SaveSettings();
                await RespondAsync($"VxInstagram is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("streamrole", "Set streamrole enabled state.")]
            [RequireOwner(Group = "Permission")]
            public async Task StreamRole(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).streamRoles = enabled;
                gd.SaveSettings();
                await RespondAsync($"StreamRole is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }

            [SlashCommand("newVxMethod", "Use the new vx method.")]
            [RequireOwner(Group = "Permission")]
            public async Task UseNewVXMethod(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).newVXMethod = enabled;
                gd.SaveSettings();
                await RespondAsync($"New VX Method is now " + (enabled ? "enabled" : "disabled") + " in this guild.", ephemeral: true);
            }
        }
    }
}
