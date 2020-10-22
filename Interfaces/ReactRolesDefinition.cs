using System;
using System.Collections.Generic;
using System.Text;

namespace TidesBotDotNet.Interfaces
{
    public class ReactRolesDefinition
    {
        public ulong messageID;
        public string emoji;
        public string role;

        public ReactRolesDefinition(ulong msgID, string emoji, string role)
        {
            this.messageID = msgID;
            this.emoji = emoji;
            this.role = role;
        }
    }
}
