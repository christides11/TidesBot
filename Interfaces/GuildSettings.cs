using System;
using System.Collections.Generic;
using System.Text;

namespace TidesBotDotNet.Interfaces
{
    public class GuildSettings
    {
        public bool colorMe = true;
        public bool vxLinks = false;
        public bool streamRoles = false;
        public HashSet<ulong> vxLinkOptOut = new HashSet<ulong>();

        public bool IsUserOptedOut(ulong userID)
        {
            return vxLinkOptOut.Contains(userID);
        }
    }
}
