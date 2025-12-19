using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
    /// <summary>
    /// HTTP delegating handler die automatisch een Bearer-token toevoegt aan uitgaande requests
    /// en bij een 401 probeert het access token te vernieuwen met behulp van een refresh token.
    /// Dit is best-effort: fouten bij ophalen/verversen worden veilig genegeerd.
    /// </summary>
    public class TokenHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;

        public TokenHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Probeer het access token uit veilige opslag te lezen en voeg het toe aan de Authorization header
                var token = await SecureStorage.Default.GetAsync("api_token");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch { /* swallow errors - geen token is niet-fataal */ }

            // Verstuur request naar server
            var response = await base.SendAsync(request, cancellationToken);

            // Als server 401 (Unauthorized) teruggeeft, probeer het token te vernieuwen (refresh flow)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                try
                {
                    var refresh = await SecureStorage.Default.GetAsync("refresh_token");
                    if (!string.IsNullOrEmpty(refresh))
                    {
                        // Vraag een nieuw access token aan via de auth-service
                        var authResult = await _authService.RefreshTokenAsync(refresh);
                        if (authResult.Success && authResult.AccessToken != null)
                        {
                            // Gebruik het nieuwe token en herhaal de request éénmaal
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                            response.Dispose();
                            response = await base.SendAsync(request, cancellationToken);
                        }
                    }
                }
                catch { /* bij mislukken: laat de oorspronkelijke 401 response teruggaan */ }
            }

            return response;
        }
    }
}
