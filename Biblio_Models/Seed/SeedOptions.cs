
// Doel: Configuratieopties voor seeding van admin gebruiker (e-mail, naam, wachtwoord).

namespace Biblio_Models.Seed
{
    public class SeedOptions
    {
        public string? AdminEmail { get; set; }
        public string? AdminPassword { get; set; }
        public string? AdminFullName { get; set; }
    }
}
