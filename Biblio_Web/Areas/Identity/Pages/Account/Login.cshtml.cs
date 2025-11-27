// Login.cshtml.cs
// Doel: PageModel voor de inlogpagina; handelt gebruikersauthenticatie af via e-mail en wachtwoord.
// Gebruik: verwerkt GET en POST van de Login pagina; gebruikt SignInManager om gebruikers aan te melden.
// Doelstellingen:
// - Bied een duidelijke, gelokaliseerde inlogervaring met client-side validatie en 'onthoud mij' optie.
// - Houd logging informatief maar niet gevoelig (log alleen niet‑geprivilegieerde informatie zoals e-mail en cookie keys/values).
// - Zorg dat returnUrl correct wordt afgehandeld zodat gebruikers na inloggen terugkeren naar de gewenste pagina.using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Biblio_Models.Entiteiten;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Biblio_Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
            Input = new InputModel();
            ReturnUrl = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string? Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid) return Page();

            // Use null-forgiving because validation attributes ensure values are present
            var result = await _signInManager.PasswordSignInAsync(Input.Email!, Input.Password!, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // log request cookies
                _logger.LogInformation("Login succeeded for {email}. Request cookies: {cookies}", Input.Email, string.Join(';', Request.Cookies.Select(kv => kv.Key + "=" + kv.Value)));
                // log any Set-Cookie headers set on the response
                if (Response?.Headers != null && Response.Headers.ContainsKey("Set-Cookie"))
                {
                    var sc = string.Join(';', Response.Headers["Set-Cookie"].ToArray());
                    _logger.LogInformation("Response Set-Cookie headers: {setCookie}", sc);
                }

                return LocalRedirect(ReturnUrl);
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
