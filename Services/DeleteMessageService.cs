using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TidesBotDotNet.Services
{
    public static class DeleteMessageService
    {

        public static void DeleteMessage(RestUserMessage msg, int time)
        {
            DeleteMsg(msg, time);
        }

        private static async Task DeleteMsg(RestUserMessage msg, int time)
        {
            await Task.Delay(time);
            await msg.DeleteAsync();
        }
    }
}
