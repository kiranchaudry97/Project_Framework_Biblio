

// Taal.cs
// Doel: entiteit die een ondersteunde taal/cultuur van de applicatie vertegenwoordigt.
// Gebruik: opgeslagen in de database en gebruikt tijdens runtime om supported cultures en default-culture te bepalen.
// Doelstellingen:
// - Bewaar korte taalcode (bijv. "nl", "en") en een gebruikersvriendelijke naam (bijv. "Nederlands").
// - Markeer één taal als standaard via `IsDefault` zodat de applicatie een default request culture kan kiezen.
// - Ondersteun soft-delete via `IsDeleted` zodat talen veilig kunnen worden uitgezet zonder historische data te verliezen.
// - Valideer input met data‑annotations zodat alleen geldige codes/waarden worden geaccepteerd.

/// <summary>
/// Representatie van een taal/cultuur die door de applicatie wordt ondersteund.
/// </summary>
/// <remarks>
/// Wordt gebruikt door de localization startup-logica om SupportedCultures en DefaultRequestCulture op te bouwen.
/// </remarks>using System.ComponentModel.DataAnnotations;

namespace Biblio_Models.Entiteiten
{
    public class Taal
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty; // e.g. "nl", "en"

        [Required]
        [MaxLength(120)]
        public string Naam { get; set; } = string.Empty; // e.g. "Nederlands", "English"

        public bool IsDefault { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}
