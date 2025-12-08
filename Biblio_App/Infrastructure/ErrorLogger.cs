using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace Biblio_App.Infrastructure
{
    internal static class ErrorLogger
    {
        private static readonly object _lock = new object();
        private static string GetLogPath()
        {
            try
            {
                var dir = FileSystem.AppDataDirectory;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "biblio_errors.log");
            }
            catch
            {
                return Path.Combine(".", "biblio_errors.log");
            }
        }

        public static void Log(Exception? ex)
        {
            try
            {
                var path = GetLogPath();
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex?.GetType().FullName}: {ex?.Message}\n{ex?.StackTrace}\n";
                lock (_lock)
                {
                    File.AppendAllText(path, text);
                }
            }
            catch { /* swallow logging errors */ }
        }

        public static void Log(string message)
        {
            try
            {
                var path = GetLogPath();
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                lock (_lock)
                {
                    File.AppendAllText(path, text);
                }
            }
            catch { }
        }
    }
}
