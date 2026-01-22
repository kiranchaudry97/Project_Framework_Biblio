// 1) //LINQ - Categorie gebruikt in queries en joins met boeken
//    Waar: seed-routine (`SeedData`) en queries in UI (bijv. `db.Categorien.FirstAsync(...)`, `Where`)
//    Doel: filteren/zoeken van categorieën, ophalen voor koppeling met `Boek`.
// 2) //lambda-expressie - predicaten/selectors bij het opvragen van categorieën
//    Waar: predicaten zoals `c => c.Naam == "Roman"` en in LINQ-expressies.
//    Doel: compacte manier om filtercriteria en sortering te definiëren.
// 3) //CRUD - Categorie neemt deel aan Create/Update/Delete via DbContext
//    Waar: `db.Categorien.AddRange(...)`, `SaveChangesAsync()` in seeding of beheer-UI.
//    Doel: beheren van categorieën in database (invoer, aanpassing, verwijderen/soft-delete)
// - Entiteit voor categorieën met validatie
// - Gebruikt in LINQ queries en soft-delete filtering// Doel: Categorie-entiteit met naam en relatie naar boeken.// Beschrijving: Bevat naam met lengtevalidatie en navigatiecollectie naar gekoppelde boeken.


namespace Biblio_Models.Entiteiten // namespace voor rolbeheer modellen
{
    public class RoleCheckbox // model voor rolselectie met checkbox
    {
        public string RoleName { get; set; } = string.Empty; // naam van de rol
        public bool IsSelected { get; set; } // status of de rol geselecteerd is met true/false
    }

    public class AdminEditRoles // model voor bewerken van gebruikersrollen in admin UI
    {
        public string UserId { get; set; } = string.Empty; // ID van de gebruiker
        public string Email { get; set; } = string.Empty; // e-mailadres van de gebruiker
        public List<RoleCheckbox> Roles { get; set; } = new List<RoleCheckbox>(); // lijst van rolselecties
    }
}
