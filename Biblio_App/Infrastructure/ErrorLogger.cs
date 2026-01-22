using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace Biblio_App.Infrastructure
{
    // =====================================================
    // ErrorLogger
    // =====================================================
    // Eenvoudige, thread-safe logging helper voor de MAUI app.
    // Wordt gebruikt om fouten lokaal naar een logbestand te schrijven,
    // handig voor debugging op mobiele toestellen waar console-logs
    // niet altijd beschikbaar zijn.
    internal static class ErrorLogger
    {
        // Lock-object om gelijktijdige schrijfacties te voorkomen
        private static readonly object _lock = new object();

        // -----------------------------------------------------
        // Bepaalt het pad naar het logbestand
        // -----------------------------------------------------
        // Probeert eerst de AppDataDirectory (platform-onafhankelijk).
        // Valt terug op de huidige directory indien nodig.
        private static string GetLogPath()
        {
            try
            {
                // MAUI-specifieke app data directory
                var dir = FileSystem.AppDataDirectory;

                // Zorg ervoor dat de directory bestaat
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Volledig pad naar het logbestand
                return Path.Combine(dir, "biblio_errors.log");
            }
            catch
            {
                // Fallback voor uitzonderlijke situaties
                return Path.Combine(".", "biblio_errors.log");
            }
        }

        // -----------------------------------------------------
        // Log een Exception
        // -----------------------------------------------------
        // Schrijft datum, type, message en stacktrace
        public static void Log(Exception? ex)
        {
            try
            {
                var path = GetLogPath();

                // Bouw logtekst op
                var text =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"{ex?.GetType().FullName}: {ex?.Message}\n" +
                    $"{ex?.StackTrace}\n";

                // Thread-safe schrijven naar bestand
                lock (_lock)
                {
                    File.AppendAllText(path, text);
                }
            }
            catch
            {
                // Logging mag de app nooit laten crashen
                // Fouten hier worden bewust genegeerd
            }
        }

        // -----------------------------------------------------
        // Log een eenvoudige tekstboodschap
        // -----------------------------------------------------
        public static void Log(string message)
        {
            try
            {
                var path = GetLogPath();

                var text =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";

                lock (_lock)
                {
                    File.AppendAllText(path, text);
                }
            }
            catch
            {
                // Ook hier: logging errors worden genegeerd
            }
        }
    }
}
