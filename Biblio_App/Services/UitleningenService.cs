using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Services
{
    public interface IUitleningenService
    {
        Task<List<Lenen>> GetUitleningenAsync(int page = 1, int pageSize = 20);
    }

    public class UitleningenService : IUitleningenService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UitleningenService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Lenen>> GetUitleningenAsync(int page = 1, int pageSize = 20)
        {
            var client = _httpClientFactory.CreateClient("ApiWithToken");
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var url = $"api/uitleningen?page={page}&pageSize={pageSize}";
            var paged = await client.GetFromJsonAsync<Biblio_App.Models.ApiPagedResult<Lenen>>(url);
            return paged?.items ?? new List<Lenen>();
        }
    }
}
