using System.ComponentModel.DataAnnotations;

namespace Biblio_Web.Models
{
    // ViewModel gebruikt voor het loginformulier
    // Bevat enkel gebruikersinput en validatieregels
    public class LoginViewModel
    {
        // E-mailadres van de gebruiker
        // Verplicht en moet een geldig e-mailadres zijn
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

        // Bepaalt of de gebruiker ingelogd blijft
        // True = persistent login cookie
        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        // URL waar de gebruiker na succesvolle login naartoe wordt gestuurd
        // Wordt meestal verborgen meegestuurd
        public string? ReturnUrl { get; set; }
    }
}
