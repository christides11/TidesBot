using System;
using System.Collections.Generic;
using System.Text;

namespace TidesBotDotNet.Interfaces
{
    public class GuildSettings
    {
        public bool colorMe = true;
        public bool vxTwitter = false;
        public bool fxTwitter = false;
        public bool vxTiktok = false;
        public bool vxShortTiktok = false;
        public bool vxInstagram = false;
        public bool streamRoles = false;
        public bool newVXMethod = false;
        public HashSet<ulong> vxLinkOptOut = new HashSet<ulong>();

        public bool IsUserOptedOutOfXV(ulong userID)
        {
            return vxLinkOptOut.Contains(userID);
        }
    }
}
