using System;
using System.IO;
using System.Text;

namespace TidesBotDotNet.Interfaces
{
    internal class Logger
    {
        public static StringBuilder LogString = new StringBuilder();

        public Logger()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("./tbot-output.txt"))
            {
                file.Write("");
                file.Close();
                file.Dispose();
            }
        }

        public static void WriteLine(string str, bool save = true)
        {
            Console.WriteLine(str);
            LogString.Append(str).Append(Environment.NewLine);
            AppendLineToLog(str);
        }

        public static void WriteLine(object val, bool save = true)
        {
            Console.WriteLine(val);
            LogString.Append(val).Append(Environment.NewLine);
            AppendLineToLog(val.ToString());
        }

        public static void Write(string str, bool save = true)
        {
            Console.Write(str);
            LogString.Append(str);
        }


        public static void AppendLineToLog(string str)
        {
            using (StreamWriter w = File.AppendText("./tbot-output.txt"))
            {
                w.WriteLine(str);
            }
        }
    }
}
