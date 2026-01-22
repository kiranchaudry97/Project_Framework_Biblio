using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Controllers.Api
{
    // API controller voor authenticatie
    // Wordt gebruikt voor login via e-mail en wachtwoord
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        // Dependency Injection van Identity services
        public AuthController(SignInManager<AppUser> signInManager,
                              UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // POST: api/auth/login
        // Login via e-mail en wachtwoord
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Basisvalidatie
            if (model == null ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Email and password are required.");
            }

            // Zoek gebruiker via e-mail
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized();
            }

            // Controleer wachtwoord (zonder cookie of token)
            var result = await _signInManager
                .CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                return Ok();
            }

            return Unauthorized();
        }

        // DTO voor login-request
        public class LoginModel
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
