using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly Biblio_Models.Data.BiblioDbContext _db;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _cfg;

        public AuthController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, Biblio_Models.Data.BiblioDbContext db, Microsoft.Extensions.Configuration.IConfiguration cfg)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _cfg = cfg;
        }

        // POST: api/auth/login (legacy - returns OK/Unauthorized)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (result.Succeeded) return Ok();
            return Unauthorized();
        }

        // POST: api/auth/token -> returns JWT + refresh token (used by MAUI client)
        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { error = "Email and password required" });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized();

            // block disabled users
            if (user is Biblio_Models.Entiteiten.AppUser au && au.IsBlocked) return Forbid();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (!result.Succeeded) return Unauthorized();

            var jwt = GenerateJwtToken(user);
            var refresh = CreateRefreshToken();

            var refreshEntity = new Biblio_Models.Entiteiten.RefreshToken
            {
                Token = refresh,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(30),
                CreatedUtc = DateTime.UtcNow,
                Revoked = false
            };

            _db.Set<Biblio_Models.Entiteiten.RefreshToken>().Add(refreshEntity);
            await _db.SaveChangesAsync();

            return Ok(new { AccessToken = jwt, RefreshToken = refresh, Expires = DateTime.UtcNow.AddMinutes(GetJwtExpiresMinutes()) });
        }

        // POST: api/auth/refresh -> exchange refresh token for new JWT + refresh
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest(new { error = "RefreshToken required" });

            var now = DateTime.UtcNow;
            var existing = await _db.Set<Biblio_Models.Entiteiten.RefreshToken>().FirstOrDefaultAsync(r => r.Token == req.RefreshToken);
            if (existing == null || existing.Revoked || existing.Expires <= now) return Unauthorized();

            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null) return Unauthorized();

            if (user is Biblio_Models.Entiteiten.AppUser au && au.IsBlocked) return Forbid();

            // Revoke old token and issue replacement
            existing.Revoked = true;
            var newRefresh = CreateRefreshToken();
            existing.ReplacedByToken = newRefresh;

            var newRefreshEntity = new Biblio_Models.Entiteiten.RefreshToken
            {
                Token = newRefresh,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(30),
                CreatedUtc = DateTime.UtcNow,
                Revoked = false
            };

            _db.Set<Biblio_Models.Entiteiten.RefreshToken>().Add(newRefreshEntity);
            _db.Set<Biblio_Models.Entiteiten.RefreshToken>().Update(existing);
            await _db.SaveChangesAsync();

            var jwt = GenerateJwtToken(user);
            return Ok(new { AccessToken = jwt, RefreshToken = newRefresh, Expires = DateTime.UtcNow.AddMinutes(GetJwtExpiresMinutes()) });
        }

        private int GetJwtExpiresMinutes()
        {
            var s = _cfg["Jwt:ExpiresMinutes"]; if (int.TryParse(s, out var m)) return m; return 60;
        }

        private string GenerateJwtToken(AppUser user)
        {
            var key = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = _cfg["Jwt:Issuer"] ?? "BiblioWebApi";
            var expires = DateTime.UtcNow.AddMinutes(GetJwtExpiresMinutes());

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? string.Empty),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
            foreach (var r in roles) claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r));

            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateRefreshToken()
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[64];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public class LoginModel
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RefreshRequest
        {
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}