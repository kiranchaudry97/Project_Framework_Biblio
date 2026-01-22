using System;

namespace Biblio_Web.Models.ApiDtos
{
    // DTO voor een boek zoals gebruikt in API-responses
    public class BoekDto
    {
        // Unieke ID van het boek
        public int Id { get; set; }

        // Titel van het boek
        public string Titel { get; set; } = string.Empty;

        // Auteur van het boek
        public string Auteur { get; set; } = string.Empty;

        // ISBN-nummer
        public string Isbn { get; set; } = string.Empty;

        // ID van de categorie waartoe het boek behoort
        public int CategorieID { get; set; }

        // Naam van de categorie (afgeplat)
        public string CategorieNaam { get; set; } = string.Empty;
    }

    // DTO voor een lid/gebruiker in API-responses
    public class LidDto
    {
        // Unieke ID van het lid
        public int Id { get; set; }

        // Voornaam van het lid
        public string Voornaam { get; set; } = string.Empty;

        // Achternaam van het lid
        public string AchterNaam { get; set; } = string.Empty;

        // E-mailadres (optioneel)
        public string? Email { get; set; }
    }

    // DTO voor een uitlening (lenen)
    public class LenenDto
    {
        // Unieke ID van de lening
        public int Id { get; set; }

        // Referentie naar het boek
        public int BoekId { get; set; }

        // Referentie naar het lid
        public int LidId { get; set; }

        // Startdatum van de lening
        public DateTime StartDate { get; set; }

        // Einddatum (vervaldatum)
        public DateTime DueDate { get; set; }

        // Datum waarop het boek werd ingeleverd (null = niet ingeleverd)
        public DateTime? ReturnedAt { get; set; }

        // Geeft aan of de lening afgesloten is
        public bool IsClosed { get; set; }

        // Optioneel: boekdetails
        public BoekDto? Boek { get; set; }

        // Optioneel: lidgegevens
        public LidDto? Lid { get; set; }
    }
}
