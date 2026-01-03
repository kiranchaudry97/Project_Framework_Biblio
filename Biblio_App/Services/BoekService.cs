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
        Task<List<Boek>> GetBoekenAsync();
    }

    public class BoekService : IBoekService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BoekService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Boek>> GetBoekenAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiWithToken");
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var result = await client.GetFromJsonAsync<List<Boek>>("api/boeken");
            return result ?? new List<Boek>();
        }
    }
}
