using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt door een admin om nieuwe gebruikers aan te maken
    // Bevat enkel formulierdata en validatieregels
    public class AdminCreateUserViewModel
    {
        // E-mailadres van de gebruiker
        // Verplicht en moet een geldig e-mailadres zijn
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Volledige naam van de gebruiker
        // Verplicht veld
        [Required]
        public string FullName { get; set; } = string.Empty;

        // Wachtwoord voor de nieuwe gebruiker
        // Wordt als password-input weergegeven
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Bepaalt of de gebruiker de Admin-rol krijgt
        public bool IsAdmin { get; set; }

        // Bepaalt of de gebruiker de Medewerker-rol krijgt
        public bool IsStaff { get; set; }
    }
}
