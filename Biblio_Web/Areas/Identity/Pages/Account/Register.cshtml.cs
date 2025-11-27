// Register.cshtml.cs
// Doel: PageModel voor de registratiepagina; behandelt het registreren van nieuwe gebruikers via e-mailadres en wachtwoord.
// Gebruik: toont het registratieformulier (GET) en verwerkt accountcreatie (POST) met UserManager en SignInManager.
// Doelstellingen:
// - Bied een toegankelijke en gelokaliseerde registratieflow met server- en client-side validatie.
// - Valideer e-mailadres en wachtwoord correct en geef duidelijke foutmeldingen terug naar de UI.
// - Log of handel fouten netjes af maar log geen gevoelige informatie (zoals wachtwoorden).
// - Zorg dat gebruiker na succesvolle registratie meteen kan worden ingelogd en teruggestuurd naar de gewenste pagina.

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
    /// PageModel voor de registratiepagina. Verwerkt het aanmaken van nieuwe gebruikers en het automatisch
    /// inloggen na succesvolle registratie (scaffolded behaviour).
    /// </summary>
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        /// <summary>
        /// Constructor: injecteert UserManager en SignInManager.
        /// </summary>
        public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Gebonden invoermodel dat de velden bevat die door de gebruiker op het registratieformulier worden ingevoerd.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        /// <summary>
        /// Url waarnaar de gebruiker na registratie moet worden teruggeleid.
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// InputModel met validatieattributen voor registratievelden.
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string? Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password")]
            public string? ConfirmPassword { get; set; }
        }

        /// <summary>
        /// GET-handler: initialiseert ReturnUrl en toont het formulier.
        /// </summary>
        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        /// <summary>
        /// POST-handler: valideert invoer en creëert een nieuw account met UserManager.
        /// Bij succes wordt de gebruiker ingelogd en geredirect naar ReturnUrl.
        /// Bij fouten worden deze toegevoegd aan ModelState zodat de view ze kan tonen.
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid) return Page();

            var email = Input.Email?.Trim();
            var user = new AppUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, Input.Password ?? "");
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(ReturnUrl);
            }

            foreach (var e in result.Errors)
            {
                // Voeg foutmeldingen toe aan ModelState zodat de view ze kan tonen
                ModelState.AddModelError(string.Empty, e.Description);
            }

            return Page();
        }
    }
}
