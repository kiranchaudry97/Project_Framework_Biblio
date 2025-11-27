// ForgotPassword.cshtml.cs
// Doel: PageModel voor de "Wachtwoord vergeten" pagina.
// Gebruik: verwerkt GET en POST requests van de ForgotPassword pagina; verzamelt het opgegeven e‑mailadres
//         en bevat de plaats waar een reset‑mail gegenereerd/gestuurd zou worden in een productieomgeving.
// Doelstellingen:
// - Bied een eenvoudige en veilige ingang voor gebruikers om een wachtwoordreset aan te vragen.
// - Valideer het e‑mailadres met data‑annotations en respecteer gelokaliseerde UI teksten.
// - Houd de implementatie overzichtelijk zodat het invoegen van e‑mail verstuur‑logica (token generatie, link, SMTP/API call)
//   later eenvoudig kan worden toegevoegd.

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Areas.Identity.Pages.Account
{
    /// <summary>
    /// PageModel voor de "Wachtwoord vergeten" pagina. Deze klasse behandelt het formulier waarin de gebruiker
    /// zijn/haar e‑mailadres invoert om een wachtwoordreset aan te vragen.
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ForgotPasswordModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Gebonden invoermodel met validatie voor het e‑mailveld.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }
        }

        /// <summary>
        /// GET handler — toont het formulier. Geen speciale logica nodig voor GET.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// POST handler — valideer invoer en zoek de gebruiker op basis van e‑mail.
        /// In een productieomgeving wordt hier een reset token gegenereerd en een reset‑link per e‑mail gestuurd.
        /// Momenteel is dit een scaffolded placeholder die simpelweg de pagina retourneert.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var user = await _userManager.FindByEmailAsync(Input.Email ?? string.Empty);
            // In een echte applicatie: genereer token en verstuur reset-email aan user wanneer deze bestaat.
            return Page();
        }
    }
}