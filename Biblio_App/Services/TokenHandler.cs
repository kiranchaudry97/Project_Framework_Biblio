using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
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
                var token = await SecureStorage.Default.GetAsync("api_token");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch { }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                try
                {
                    var refresh = await SecureStorage.Default.GetAsync("refresh_token");
                    if (!string.IsNullOrEmpty(refresh))
                    {
                        var authResult = await _authService.RefreshTokenAsync(refresh);
                        if (authResult.Success && authResult.AccessToken != null)
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                            response.Dispose();
                            response = await base.SendAsync(request, cancellationToken);
                        }
                    }
                }
                catch { }
            }

            return response;
        }
    }
}
