using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Biblio_App.Services
{
    // API-based provider: haalt tellerwaarden op via een Web API endpoint.
    public class ApiGegevensProvider : IGegevensProvider
    {
        private readonly HttpClient _httpClient;

        public ApiGegevensProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        private class TellersDto
        {
            [JsonPropertyName("boeken")] public int Boeken { get; set; }
            [JsonPropertyName("leden")] public int Leden { get; set; }
            [JsonPropertyName("openUitleningen")] public int OpenUitleningen { get; set; }
        }

        public async Task<(int boeken, int leden, int openUitleningen)> GetTellersAsync()
        {
            try
            {
                // Verwacht endpoint: GET /api/dashboard/tellers
                var dto = await _httpClient.GetFromJsonAsync<TellersDto>("api/dashboard/tellers");
                if (dto == null) return (0, 0, 0);
                return (dto.Boeken, dto.Leden, dto.OpenUitleningen);
            }
            catch (Exception)
            {
                // Bij netwerkfouten of parsing-fouten: terugvallen op 0
                return (0, 0, 0);
            }
        }
    }
}
