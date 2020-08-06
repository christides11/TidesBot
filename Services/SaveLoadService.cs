using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TidesBotDotNet.Services
{
    public static class SaveLoadService
    {

        public static void Save(string fileName, string jsonObject)
        {
            try
            {
                using (StreamWriter streamWriter = File.CreateText(Path.Combine(Directory.GetCurrentDirectory(), fileName)))
                {
                    streamWriter.Write(jsonObject);
                }
            }catch(Exception e)
            {
                Console.WriteLine($"Exception thrown while saving {fileName}. {e.Message}");
            }
        }

        public static string Load(string path)
        {
            try
            {
                string p = Path.Combine(Directory.GetCurrentDirectory(), path);
                if (File.Exists(p))
                {
                    string jsonString = null;
                    using (StreamReader streamReader = File.OpenText(p))
                    {
                        jsonString = streamReader.ReadToEnd();
                    }
                    return jsonString;
                }

            }catch(Exception e)
            {
                Console.WriteLine($"Exception thrown while loading {path}. {e.Message}");
            }
            return null;
        }
    }
}
