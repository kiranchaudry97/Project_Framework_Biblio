using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Services
{
    public interface IBoekService
    {
        // Haal boeken op via de Web API (wordt vooral gebruikt als online bron).
        Task<List<Boek>> GetBoekenAsync();
    }

    public class BoekService : IBoekService
    {
        // HttpClientFactory levert een vooraf geconfigureerde HttpClient (zie `MauiProgram.cs`)
        private readonly IHttpClientFactory _httpClientFactory;

        public BoekService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Boek>> GetBoekenAsync()
        {
            // 1) Maak de API client
            var client = _httpClientFactory.CreateClient("ApiWithToken");

            // 2) Haal token uit SecureStorage en voeg toe aan Authorization header
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 3) API endpoint geeft een paged resultaat terug:
            // { page, pageSize, total, totalPages, items: [...] }
            // let op: dit is enkel GET. Voor echte CRUD gebruikt de app vooral lokale opslag (offline-first)
            var paged = await client.GetFromJsonAsync<Biblio_App.Models.ApiPagedResult<Boek>>("api/boeken");
            return paged?.items ?? new List<Boek>();
        }
    }
}
