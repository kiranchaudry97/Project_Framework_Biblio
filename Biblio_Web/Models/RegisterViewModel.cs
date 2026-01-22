using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt voor het registratieformulier
    // Bevat enkel data en validatieregels (geen database-logica)
    public class RegisterViewModel
    {
        // Volledige naam van de gebruiker
        // Verplicht veld, label komt uit resourcebestand
        [Required]
        [Display(Name = "FullName")]
        public string? FullName { get; set; }

        // E-mailadres van de gebruiker
        // Moet een geldig e-mailadres zijn
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Wachtwoord van de gebruiker
        // Wordt als password-input weergegeven
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        // Bevestiging van het wachtwoord
        // Moet exact overeenkomen met Password
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "ConfirmPassword")]
        public string? ConfirmPassword { get; set; }
    }
}
