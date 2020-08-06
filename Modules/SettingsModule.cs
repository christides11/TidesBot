using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TidesBotDotNet.Interfaces;
using TidesBotDotNet.Services;

namespace TidesBotDotNet.Modules
{
    [Group("settings")]
    public class SettingsModule : ModuleBase<SocketCommandContext>
    {
        public GuildsDefinition gd;

        [Group("user")]
        public class SettingsUserModule : ModuleBase<SocketCommandContext>
        {
            public GuildsDefinition gd;

            public SettingsUserModule(GuildsDefinition guildsDefinition)
            {
                gd = guildsDefinition;
            }

            [Command("colorme")]
            public async Task ColorMe()
            {
                await Context.Channel
                    .SendMessageAsync($"Colorme permission is set to {gd.GetSettings(Context.Guild.Id).colorMe.ToString()}.");
            }

            [Command("colorme")]
            public async Task ColorMe(bool enabled)
            {
                gd.GetSettings(Context.Guild.Id).colorMe = enabled;
                await Context.Channel.SendMessageAsync($"ColorMe set to " + ( enabled ? "enabled" : "disabled" ) + ".");
                gd.SaveSettings();
            }
        }
    }
}
