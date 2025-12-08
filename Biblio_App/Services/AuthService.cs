using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Biblio_App.Models;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private string? _token;

        public AuthService(HttpClient http) => _http = http;

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("api/auth/token", new { Email = email, Password = password });
                if (!resp.IsSuccessStatusCode)
                {
                    return new AuthResult();
                }
                var obj = await resp.Content.ReadFromJsonAsync<AuthResult>();
                if (obj?.AccessToken != null)
                {
                    _token = obj.AccessToken;
                    try { Preferences.Default.Set("api_token", _token); } catch { }
                }
                return obj ?? new AuthResult();
            }
            catch
            {
                return new AuthResult();
            }
        }

        public void Logout()
        {
            _token = null;
            try { Preferences.Default.Remove("api_token"); } catch { }
        }

        public string? GetToken()
        {
            if (!string.IsNullOrEmpty(_token)) return _token;
            try { _token = Preferences.Default.Get("api_token", string.Empty); } catch { }
            return string.IsNullOrEmpty(_token) ? null : _token;
        }
    }
}
