namespace Biblio_Web.ApiModels
{
    public class LidDto
    {
        public int Id { get; set; }
        public string Voornaam { get; set; } = string.Empty;
        public string AchterNaam { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefoon { get; set; }
        public string? Adres { get; set; }
    }
}