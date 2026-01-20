using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Services
{
    public interface ILedenService
    {
        // Haal leden op via de Web API (online bron). In offline mode leest de app uit SQLite.
        Task<List<Lid>> GetLedenAsync();
    }

    public class LedenService : ILedenService
    {
        // HttpClientFactory levert een geconfigureerde client met BaseAddress
        private readonly IHttpClientFactory _httpClientFactory;

        public LedenService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Lid>> GetLedenAsync()
        {
            // 1) Maak API client
            var client = _httpClientFactory.CreateClient("ApiWithToken");

            // 2) Voeg Bearer token toe (indien aanwezig)
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 3) API geeft paged resultaat terug
            var paged = await client.GetFromJsonAsync<Biblio_App.Models.ApiPagedResult<Lid>>("api/leden");
            return paged?.items ?? new List<Lid>();
        }
    }
}
