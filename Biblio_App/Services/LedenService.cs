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
        Task<List<Lid>> GetLedenAsync();
    }

    public class LedenService : ILedenService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LedenService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Lid>> GetLedenAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiWithToken");
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var result = await client.GetFromJsonAsync<List<Lid>>("api/leden");
            return result ?? new List<Lid>();
        }
    }
}
