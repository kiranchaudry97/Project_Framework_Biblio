using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    // ViewModel voor het wijzigen of resetten van een wachtwoord
    // Ondersteunt zowel gewone gebruikers als admins
    public class ChangePasswordViewModel
    {
        // Unieke ID van de gebruiker (Identity)
        public string UserId { get; set; } = string.Empty;

        // Gebruikersnaam (informatief, wordt getoond in de UI)
        public string UserName { get; set; } = string.Empty;

        // Huidig wachtwoord
        // Enkel vereist wanneer een gebruiker zijn eigen wachtwoord wijzigt
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        // Nieuw wachtwoord
        // Verplicht en minimum 8 tekens
        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        // Bevestiging van het nieuwe wachtwoord
        // Moet overeenkomen met NewPassword
        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string? ConfirmPassword { get; set; }

        // Geeft aan of een admin het wachtwoord van een andere gebruiker reset
        // Wordt gebruikt om de juiste Identity-methode te kiezen
        public bool IsAdminChange { get; set; }
    }
}
