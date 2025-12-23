using System;

namespace Biblio_Web.Models.ApiDtos
{
    public class BoekDto
    {
        public int Id { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string Auteur { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public int CategorieID { get; set; }
        public string CategorieNaam { get; set; } = string.Empty;
    }

    public class LidDto
    {
        public int Id { get; set; }
        public string Voornaam { get; set; } = string.Empty;
        public string AchterNaam { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class LenenDto
    {
        public int Id { get; set; }
        public int BoekId { get; set; }
        public int LidId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public bool IsClosed { get; set; }

        public BoekDto? Boek { get; set; }
        public LidDto? Lid { get; set; }
    }
}
