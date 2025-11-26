using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Biblio_Models.Entiteiten;
using System.Threading.Tasks;

namespace Biblio_Web.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        public ForgotPasswordModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var email = Input.Email?.Trim() ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(email);
            // do not reveal whether email exists
            if (user == null)
            {
                StatusMessage = "If the email exists, a reset link will be sent.";
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // In development show token; in production send email
            StatusMessage = "Reset token (dev): " + token;
            return Page();
        }
    }
}
