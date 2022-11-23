using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AL_Local_Mapper_Core
{
    internal class Logger
    {
        public void Debug(String text) => Log(text, Classes.LogLevel.Debug);
        public void Info(String text) => Log(text, Classes.LogLevel.Info);
        public void Warning(String text) => Log(text, Classes.LogLevel.Warning);
        public void Warn(String text) => Warning(text);

        public void Warning(String text, Exception ex)
        {
            Log(text, Classes.LogLevel.Warning, false);
            WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] {text}\n\t{ExceptionToString(ex, "\t")}", Classes.LogLevel.Warning);
        }

        public void Warn(String text, Exception ex) => Warning(text, ex);

        public void Error(String text) => Log(text, Classes.LogLevel.Error);

        public void Error(String text, Exception ex)
        {
            Log(text, Classes.LogLevel.Error, false);
            WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {text}\n\t{ExceptionToString(ex, "\t")}", Classes.LogLevel.Error);
        }

        private static String ExceptionToString(Exception ex, String padding = "")
        {
            String str = $"{padding}{ex.Message}\n\tStacktrace: {ex.StackTrace}\n";
            if (ex.Data != null && ex.Data.Count > 0)
            {
                str += $"{padding}\tData:\n";
                foreach (Object key in ex.Data)
                    str += $"{padding}\t\t{key}: {ex.Data[key]}\n";
            }

            if (ex.InnerException != null)
            {
                str += $"{padding}\tInner Exception:\n{ExceptionToString(ex.InnerException, $"{padding}\t")}";
            }

            return str;
        }

        private void Log(String text, Classes.LogLevel level, bool writeTofile = true)
        {
            try
            {
                String str = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpper()}] {text}";
                Console.WriteLine(str);
                System.Diagnostics.Debug.WriteLine(str);

                if (writeTofile)
                    WriteToFile(str, level);
            }
            catch { }
        }

        private void WriteToFile(String text, Classes.LogLevel level)
        {
            if ((int)level >= (int)Glob.Settings.LogLevel)
            {
                try
                {
                    File.AppendAllText(Path.Combine(Glob.Settings.LogPath, $"log_{DateTime.Now:yyyy-MM-dd}.txt"), $"{text}\n");
                }
                catch { }
            }
        }
    }
}
