// Doel: Configuratieopties voor seeding van admin gebruiker (e-mail, naam, wachtwoord).
// zie commit bericht

namespace Biblio_Models.Seed
{
    public class SeedOptions
    {
        public string? AdminEmail { get; set; }
        public string? AdminPassword { get; set; }
        public string? AdminFullName { get; set; }

        // Optionele test accounts (configureer via User Secrets / appsettings)
        public bool CreateTestAccounts { get; set; } = false; // false in productie per default
        public string? StaffEmail { get; set; }
        public string? StaffPassword { get; set; }
        public string? BlockedEmail { get; set; }
        public string? BlockedPassword { get; set; }
    }
}
