namespace Biblio_Web.ApiModels
{
    public class BoekDto
    {
        public int Id { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string Auteur { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public int CategorieID { get; set; }
        public string? CategorieNaam { get; set; }
    }
}