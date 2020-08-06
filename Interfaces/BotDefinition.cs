using System;
using System.Collections.Generic;
using System.Text;

namespace TidesBotDotNet.Interfaces
{
    public class BotDefinition
    {
        public class BotDef
        {
            public string token;
            public string status;
            public string prefix;
        }

        public int selectedDefinition = 0;
        public List<BotDef> botDefinitions = new List<BotDef>();

        public BotDef GetCurrent()
        {
            return botDefinitions[selectedDefinition];
        }
    }
}
