using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using Biblio_Models.Entiteiten;
using System.Threading;
using System;
using System.Diagnostics;

namespace Biblio_App.Services
{
    public interface IUitleningenService
    {
        // Haal uitleningen op via de Web API (met paging).
        // In offline-first scenario blijft de app werken via lokale DB.
        Task<List<Lenen>> GetUitleningenAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }

    public class UitleningenService : IUitleningenService
    {
        // HttpClientFactory levert de API client (BaseAddress, timeout, cert policies)
        private readonly IHttpClientFactory _httpClientFactory;

        public UitleningenService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Lenen>> GetUitleningenAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            // 1) Maak API client
            var client = _httpClientFactory.CreateClient("ApiWithToken");

            // 2) Voeg token toe aan Authorization header (als user ingelogd is)
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 3) Stel url samen met paging parameters
            var url = $"api/uitleningen?page={page}&pageSize={pageSize}";

            try
            {
                // Probeer data via API op te halen.
                // Bij succes krijgen we terug: { items: [...] }
                var paged = await client.GetFromJsonAsync<Biblio_App.Models.ApiPagedResult<Lenen>>(url, cancellationToken);
                return paged?.items ?? new List<Lenen>();
            }
            catch (OperationCanceledException ex)
            {
                // OperationCanceledException = request is geannuleerd of timeout (via cancellationToken)
                try { Debug.WriteLine($"GetUitleningenAsync cancelled: {ex.Message}"); } catch { }
                return new List<Lenen>();
            }
            catch (HttpRequestException)
            {
                // Netwerkfout (geen internet, server down, DNS, ...)
                return new List<Lenen>();
            }
            catch (Exception)
            {
                // Andere onverwachte fout (JSON parse, etc.)
                return new List<Lenen>();
            }
        }
    }
}
