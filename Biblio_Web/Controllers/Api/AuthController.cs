using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Biblio_Models.Entiteiten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Controllers.Api
{
    // zie commit
    // Deze controller verzorgt authenticatie en het uitgeven van JWT-tokens voor API-gebruikers.
    // Kort: accepteert e-mail en wachtwoord, valideert gebruiker en retourneert een access token met rollen.
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        /// <summary>
        /// Maakt een JWT access token aan voor een gebruiker op basis van e-mail en wachtwoord.
        /// Retourneert HTTP 400 bij ontbrekende gegevens, 401 bij ongeldige inloggegevens,
        /// en 200 met het token en vervaltijd bij succesvolle authenticatie.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] LoginRequest request)
        {
            // Validatie van inkomende request
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            // Zoek gebruiker op e-mail
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized();
            }

            // Controleer wachtwoord
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Unauthorized();
            }

            // Haal rollen op om deze als claims in het token op te nemen
            var roles = await _userManager.GetRolesAsync(user);

            // Lees JWT-configuratie (met defaults voor development)
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"] ?? _config["Jwt:Key"] ?? "SuperSecretDevelopmentKey";
            var issuer = jwtSection["Issuer"] ?? _config["Jwt:Issuer"] ?? "BiblioApp";
            var expiresMinutes = 60;
            if (int.TryParse(jwtSection["ExpiresMinutes"], out var m)) expiresMinutes = m;

            // Bouw standaardclaims (subject, naam, e-mail)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            // Voeg rolclaims toe
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            // Maak signing key en credentials
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Stel vervaltijd in
            var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

            // Maak het JWT token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Retourneer token en vervaltijd
            return Ok(new { access_token = tokenString, expires = expires });
        }
    }

    /// <summary>
    /// DTO voor het ontvangen van de inloggegevens in de token-aanvraag.
    /// Velden zijn nullable om model binding fouten duidelijk af te handelen.
    /// </summary>
    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}