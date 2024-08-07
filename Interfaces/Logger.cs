using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidesBotDotNet.Interfaces
{
    internal class Logger
    {
        public static StringBuilder LogString = new StringBuilder();

        public Logger()
        {
        }

        public static void WriteLine(string str, bool save = true)
        {
            Console.WriteLine(str);
            LogString.Append(str).Append(Environment.NewLine);
            if (save) SaveLog();
        }

        public static void WriteLine(object val, bool save = true)
        {
            Console.WriteLine(val);
            LogString.Append(val).Append(Environment.NewLine);
            if (save) SaveLog();
        }

        public static void Write(string str, bool save = true)
        {
            Console.Write(str);
            LogString.Append(str);
            if(save) SaveLog();
        }

        public static void SaveLog()
        {
            if (LogString != null && LogString.Length > 0)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("./tbot-output.txt"))
                {
                    file.Write(LogString.ToString());
                    file.Close();
                    file.Dispose();
                }
            }
        }
    }
}
