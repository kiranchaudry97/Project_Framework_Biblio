using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using Biblio_Models.Entiteiten;

namespace Biblio_App.Services
{
    /// <summary>
    /// ILedenService
    /// 
    /// Servicecontract voor het ophalen van leden.
    /// 
    /// Doel:
    /// - De MAUI app abstraheren van de bron (API vs lokaal)
    /// - Later eenvoudig uitbreidbaar naar offline-first scenario’s
    /// 
    /// In deze implementatie:
    /// - Online: data komt van de Web API
    /// - Offline (optioneel): kan later via SQLite
    /// </summary>
    public interface ILedenService
    {
        /// <summary>
        /// Haalt een lijst van leden op.
        /// 
        /// - In online modus: via Web API
        /// - In offline modus: uit lokale SQLite DB (niet hier geïmplementeerd)
        /// </summary>
        /// <returns>Lijst van Lid-entiteiten</returns>
        Task<List<Lid>> GetLedenAsync();
    }

    /// <summary>
    /// LedenService
    /// 
    /// Concrete implementatie van ILedenService die:
    /// - een HttpClient gebruikt (via IHttpClientFactory)
    /// - authenticatie toepast met Bearer token
    /// - data ophaalt via de REST API
    /// </summary>
    public class LedenService : ILedenService
    {
        /// <summary>
        /// IHttpClientFactory wordt gebruikt i.p.v. rechtstreeks HttpClient,
        /// zodat:
        /// - BaseAddress
        /// - default headers
        /// - handlers (auth, logging, retry)
        /// 
        /// centraal geconfigureerd kunnen worden in MauiProgram.cs
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Constructor
        /// 
        /// De HttpClientFactory wordt via Dependency Injection aangeleverd.
        /// </summary>
        public LedenService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Haalt alle leden op via de Web API.
        /// 
        /// Technische flow:
        /// 1) Maak een HttpClient aan (met BaseAddress)
        /// 2) Voeg Bearer token toe indien beschikbaar
        /// 3) Roep API endpoint /api/leden aan
        /// 4) Lees het gepagineerde resultaat
        /// 5) Geef enkel de items (leden) terug
        /// </summary>
        public async Task<List<Lid>> GetLedenAsync()
        {
            // 1) Maak een vooraf geconfigureerde HttpClient aan
            //    "ApiWithToken" is geregistreerd in MauiProgram.cs
            var client = _httpClientFactory.CreateClient("ApiWithToken");

            // 2) Haal het opgeslagen JWT token op uit SecureStorage
            //    (token wordt gezet bij login)
            var token = await SecureStorage.GetAsync("auth_token");

            // Als er een token is, voeg deze toe als Authorization header
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            // 3) API retourneert een gepagineerd resultaat:
            // {
            //   page,
            //   pageSize,
            //   total,
            //   totalPages,
            //   items: [...]
            // }
            var paged = await client.GetFromJsonAsync<
                Biblio_App.Models.ApiPagedResult<Lid>>("api/leden");

            // 4) Geef enkel de ledenlijst terug
            //    (null-safe fallback naar lege lijst)
            return paged?.items ?? new List<Lid>();
        }
    }
}
