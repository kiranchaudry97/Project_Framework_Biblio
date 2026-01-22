using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Biblio_App.Models;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
    /// <summary>
    /// AuthService
    /// 
    /// Verantwoordelijk voor:
    /// - authenticatie tegen de Web API
    /// - opslaan en ophalen van JWT access tokens
    /// - refresh token flow
    /// 
    /// Wordt gebruikt door ViewModels (MVVM),
    /// niet rechtstreeks door UI (Views).
    /// </summary>
    public class AuthService : IAuthService
    {
        // HttpClient voor communicatie met de API
        // Wordt via Dependency Injection aangeleverd
        private readonly HttpClient _http;

        // In-memory cache van het access token
        // Vermijdt onnodige SecureStorage reads
        private string? _token;

        /// <summary>
        /// Constructor – HttpClient wordt via DI geïnjecteerd
        /// </summary>
        public AuthService(HttpClient http) => _http = http;

        /// <summary>
        /// Logt de gebruiker in via e-mail en wachtwoord.
        /// 
        /// Flow:
        /// 1) POST naar api/auth/token
        /// 2) Ontvang AuthResult (access + refresh token)
        /// 3) Bewaar tokens in SecureStorage
        /// </summary>
        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                // Stuur login request naar API
                var resp = await _http.PostAsJsonAsync(
                    "api/auth/token",
                    new { Email = email, Password = password });

                // Als login faalt (401 / 400 / 500)
                if (!resp.IsSuccessStatusCode)
                {
                    return new AuthResult();
                }

                // JSON response omzetten naar AuthResult
                var obj = await resp.Content.ReadFromJsonAsync<AuthResult>();

                // Als access token aanwezig is → opslaan
                if (obj?.AccessToken != null)
                {
                    // In-memory cache
                    _token = obj.AccessToken;

                    // Opslaan in SecureStorage (veilig per platform)
                    try
                    {
                        await SecureStorage.Default.SetAsync("api_token", _token);
                    }
                    catch { }

                    try
                    {
                        await SecureStorage.Default.SetAsync("refresh_token", obj.RefreshToken ?? string.Empty);
                    }
                    catch { }
                }

                return obj ?? new AuthResult();
            }
            catch
            {
                // Geen exceptions laten doorlekken naar UI
                return new AuthResult();
            }
        }

        /// <summary>
        /// Logt de gebruiker uit.
        /// 
        /// Verwijdert:
        /// - in-memory token
        /// - SecureStorage tokens
        /// </summary>
        public void Logout()
        {
            // In-memory token wissen
            _token = null;

            // SecureStorage opschonen
            try
            {
                SecureStorage.Default.SetAsync("api_token", string.Empty).Wait();
            }
            catch { }

            try
            {
                SecureStorage.Default.SetAsync("refresh_token", string.Empty).Wait();
            }
            catch { }
        }

        /// <summary>
        /// Haalt het huidige access token op.
        /// 
        /// Eerst uit geheugen (sneller),
        /// anders uit SecureStorage.
        /// </summary>
        public string? GetToken()
        {
            // 1) Gebruik cached token indien beschikbaar
            if (!string.IsNullOrEmpty(_token))
                return _token;

            // 2) Anders proberen uit SecureStorage te lezen
            try
            {
                _token = SecureStorage.Default.GetAsync("api_token").Result;
            }
            catch { }

            return string.IsNullOrEmpty(_token) ? null : _token;
        }

        /// <summary>
        /// Vraagt een nieuw access token aan via een refresh token.
        /// 
        /// Wordt typisch gebruikt wanneer:
        /// - API 401 teruggeeft
        /// - access token verlopen is
        /// </summary>
        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Refresh token request naar API
                var resp = await _http.PostAsJsonAsync(
                    "api/auth/refresh",
                    new { RefreshToken = refreshToken });

                // Als refresh faalt → leeg resultaat
                if (!resp.IsSuccessStatusCode)
                    return new AuthResult();

                // Lees nieuwe tokens
                var obj = await resp.Content.ReadFromJsonAsync<AuthResult>();

                if (obj?.AccessToken != null)
                {
                    // Update in-memory cache
                    _token = obj.AccessToken;

                    // Update SecureStorage
                    try
                    {
                        await SecureStorage.Default.SetAsync("api_token", _token);
                    }
                    catch { }

                    try
                    {
                        await SecureStorage.Default.SetAsync("refresh_token", obj.RefreshToken ?? string.Empty);
                    }
                    catch { }
                }

                return obj ?? new AuthResult();
            }
            catch
            {
                return new AuthResult();
            }
        }
    }
}
