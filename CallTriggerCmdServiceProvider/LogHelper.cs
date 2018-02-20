using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TCX.CallTriggerCmd
{
    public class LogHelper
    {
        private static object lockObj = new object();

        public static void Log(Environment.SpecialFolder specialFolder, string fileName, string text)
        {
            lock (lockObj)
            {
                try
                {
                    string directoryName = Path.Combine(Environment.GetFolderPath(specialFolder), @"CallTriggerCmd\Logs");
                    if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
                    File.AppendAllText(Path.Combine(directoryName, fileName), DateTime.Now.ToString() + " - " + text + Environment.NewLine);
                }
                catch (Exception)
                {
                    // Silently ignore this error...
                }
            }
        }
    }
}
