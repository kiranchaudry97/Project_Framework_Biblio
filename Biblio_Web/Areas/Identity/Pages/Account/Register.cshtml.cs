using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Biblio_Models.Entiteiten;
using Biblio_Web.Models;
using System.Threading.Tasks;

namespace Biblio_Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new RegisterViewModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var email = Input.Email ?? string.Empty;
            var password = Input.Password ?? string.Empty;

            var user = new AppUser { UserName = email, Email = email, FullName = Input.FullName };
            var res = await _userManager.CreateAsync(user, password);
            if (res.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Lid");
                await _signInManager.SignInAsync(user, false);
                return LocalRedirect("~/");
            }

            foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }
    }
}
