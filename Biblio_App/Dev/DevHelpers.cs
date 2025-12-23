using System.IO;
using Microsoft.Maui.Storage;

namespace Biblio_App.Dev
{
    internal static class DevHelpers
    {
        public static void DeleteLocalDbIfExists()
        {
#if DEBUG
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "biblio.db");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // swallow exceptions in dev helper
            }
#endif
        }
    }
}
