/*
API endpoints (AuthController)
- POST /api/auth/token       -> aanvraag van JWT: { "email": "..", "password": ".." }  (retourneert { access_token, expires })
- POST /api/auth/register    -> registratie van gebruiker: { "email": "..", "password": "..", "fullname": ".." } (retourneert { userId })
- POST /api/auth/confirm     -> bevestiging van e-mailadres: { "userId": "..", "token": ".." } (retourneert { message })
*/

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
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
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

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
        private readonly IEmailSender _emailSender;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration config, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Maakt een JWT access token aan voor een gebruiker op basis van e-mail en wachtwoord.
        /// Retourneert HTTP 400 bij ontbrekende gegevens, 401 bij ongeldige inloggegevens,
        /// en 200 met het token en vervaltijd bij succesvolle authenticatie.
        /// </summary>
        // POST: api/auth/token (aanvraag token)
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

            // Prevent login if account is blocked
            if (user.IsBlocked)
            {
                return Forbid();
            }

            // Prevent issuing token if email not confirmed and application requires it
            var requireConfirmed = _userManager.Options.SignIn.RequireConfirmedAccount;
            if (requireConfirmed && !user.EmailConfirmed)
            {
                return Forbid();
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

            // Generate refresh token and store it
            var refreshTokenBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(refreshTokenBytes);
            var refreshTokenString = Convert.ToBase64String(refreshTokenBytes);
            var refresh = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                CreatedUtc = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(30),
                Revoked = false
            };

            // store refresh token using application DbContext
            try
            {
                // Resolve BiblioDbContext from HttpContext request services
                var db = HttpContext.RequestServices.GetService(typeof(Biblio_Models.Data.BiblioDbContext)) as Biblio_Models.Data.BiblioDbContext;
                if (db != null)
                {
                    db.Set<RefreshToken>().Add(refresh);
                    await db.SaveChangesAsync();
                }
            }
            catch { }

            // Retourneer token en vervaltijd inclusief refresh token
            return Ok(new { access_token = tokenString, refresh_token = refreshTokenString, expires = expires });
        }

        /// <summary>
        /// API registratie endpoint voor mobiele/remote clients.
        /// Creëert een nieuwe gebruiker (AppUser) en stuurt een bevestigings-e-mail (development: logged).
        /// Geen roltoekenning gebeurt automatisch.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = false // require confirmation
            };

            var res = await _userManager.CreateAsync(user, model.Password);
            if (!res.Succeeded)
            {
                var errors = res.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { errors });
            }

            // generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // If IEmailSender is available, send a confirmation message that includes token and instructions.
            try
            {
                var frontendUrl = _config["AppSettings:FrontendBaseUrl"] ?? _config["FrontendBaseUrl"] ?? string.Empty;

                var confirmationInfo = string.Empty;
                if (!string.IsNullOrWhiteSpace(frontendUrl))
                {
                    // construct a friendly link for front-end to call confirm endpoint (could be a page or API route)
                    var encoded = System.Web.HttpUtility.UrlEncode(token);
                    var link = frontEndLink(frontendUrl, user.Id, encoded);
                    confirmationInfo = $"Please confirm your account by visiting: {link}";
                }
                else
                {
                    confirmationInfo = $"Confirmation token: {token} (use api/auth/confirm to confirm)";
                }

                await _emailSender.SendEmailAsync(user.Email ?? string.Empty, "Confirm your Biblio account", confirmationInfo);
            }
            catch
            {
                // ignore email send failures in API; account created but user cannot confirm via email
            }

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new { userId = user.Id });

            static string frontEndLink(string frontendBase, string userId, string encodedToken)
            {
                // prefer frontend route that accepts userId & token (client should POST to api/auth/confirm)
                // return a URL with query string so client-side apps can extract and call API confirm
                return frontendBase.TrimEnd('/') + $"/?confirmUserId={userId}&confirmToken={encodedToken}";
            }
        }

        /// <summary>
        /// Confirm email using userId and token. This endpoint allows mobile clients to confirm email tokens.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.Token))
            {
                return BadRequest("UserId and token are required.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (result.Succeeded)
            {
                return Ok(new { message = "Email confirmed." });
            }

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken)) return BadRequest("Refresh token is required.");

            try
            {
                var db = HttpContext.RequestServices.GetService(typeof(Biblio_Models.Data.BiblioDbContext)) as Biblio_Models.Data.BiblioDbContext;
                if (db == null) return StatusCode(500);

                var existing = await db.Set<RefreshToken>().FirstOrDefaultAsync(r => r.Token == model.RefreshToken && !r.Revoked && r.Expires > DateTime.UtcNow);
                if (existing == null) return Unauthorized();

                var user = await _userManager.FindByIdAsync(existing.UserId);
                if (user == null) return Unauthorized();

                // Issue new access token
                var jwtSection = _config.GetSection("Jwt");
                var key = jwtSection["Key"] ?? _config["Jwt:Key"] ?? "SuperSecretDevelopmentKey";
                var issuer = jwtSection["Issuer"] ?? _config["Jwt:Issuer"] ?? "BiblioApp";
                var expiresMinutes = 60;
                if (int.TryParse(jwtSection["ExpiresMinutes"], out var m)) expiresMinutes = m;

                var roles = await _userManager.GetRolesAsync(user);
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
                };
                foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

                var token = new JwtSecurityToken(issuer: issuer, audience: issuer, claims: claims, expires: expires, signingCredentials: creds);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // rotate refresh token: revoke existing and create a new one
                existing.Revoked = true;
                existing.ReplacedByToken = Guid.NewGuid().ToString("N");
                var newRefreshBytes = new byte[64];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(newRefreshBytes);
                var newRefreshToken = Convert.ToBase64String(newRefreshBytes);

                var refresh = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = existing.UserId,
                    CreatedUtc = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(30),
                    Revoked = false
                };

                db.Set<RefreshToken>().Add(refresh);
                await db.SaveChangesAsync();

                return Ok(new { access_token = tokenString, refresh_token = newRefreshToken, expires = expires });
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequest model)
        {
            if (model == null) return BadRequest("Invalid request.");

            var db = HttpContext.RequestServices.GetService(typeof(Biblio_Models.Data.BiblioDbContext)) as Biblio_Models.Data.BiblioDbContext;
            if (db == null) return StatusCode(500);

            // If a specific refresh token is provided, revoke it
            if (!string.IsNullOrWhiteSpace(model.RefreshToken))
            {
                var existing = await db.Set<RefreshToken>().FirstOrDefaultAsync(r => r.Token == model.RefreshToken && !r.Revoked && r.Expires > DateTime.UtcNow);
                if (existing == null) return NotFound(new { message = "Refresh token not found or already revoked/expired." });

                // Only allow owner or admin to revoke
                var callerId = User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var callerIsAdmin = User?.IsInRole("Admin") ?? false;
                if (!callerIsAdmin && !string.Equals(callerId, existing.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                existing.Revoked = true;
                await db.SaveChangesAsync();
                return Ok(new { message = "Refresh token revoked." });
            }

            // Otherwise revoke all tokens for the current user (or specified userId if caller is admin)
            string targetUserId = null;
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                // only admins may revoke tokens for other users
                if (!(User?.IsInRole("Admin") ?? false)) return Forbid();
                targetUserId = model.UserId;
            }
            else
            {
                targetUserId = User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(targetUserId)) return Forbid();
            }

            var tokens = await db.Set<RefreshToken>().Where(r => r.UserId == targetUserId && !r.Revoked).ToListAsync();
            if (tokens.Count == 0) return NotFound(new { message = "No active refresh tokens found for user." });

            foreach (var t in tokens) t.Revoked = true;
            await db.SaveChangesAsync();

            return Ok(new { message = "All refresh tokens revoked for user." });
        }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
    }

    public class ConfirmEmailRequest
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
    }

    public class RefreshRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class RevokeRequest
    {
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
    }
}