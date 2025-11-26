using System;
using System.Resources;
using System.Globalization;

namespace Biblio_Models.Resources
{
    // Resource wrapper exposing strongly-typed resource properties used by DataAnnotations
    public static class SharedModelResource
    {
        private static ResourceManager? _rm;
        private static ResourceManager ResourceManager => _rm ??= new ResourceManager("Biblio_Models.Resources.SharedModelResource", typeof(SharedModelResource).Assembly);

        private static string? TryGetString(string name)
        {
            try
            {
                return ResourceManager.GetString(name, CultureInfo.CurrentUICulture);
            }
            catch (MissingManifestResourceException)
            {
                // Resource not found — fall back to defaults
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string Required => TryGetString("Required") ?? "The {0} field is required.";
        public static string StringLength => TryGetString("StringLength") ?? "The field {0} must be a string with a maximum length of {1}.";
        public static string EmailAddress => TryGetString("EmailAddress") ?? "The {0} field is not a valid e-mail address.";
        public static string Phone => TryGetString("Phone") ?? "The {0} field is not a valid phone number.";
        public static string IsbnInvalid => TryGetString("IsbnInvalid") ?? "The ISBN is invalid.";
    }
}