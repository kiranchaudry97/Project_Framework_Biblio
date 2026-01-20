// Basis .NET namespaces voor async/await, cancellation en basis types
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

// MAUI storage API: om een marker/logbestand platform-onafhankelijk weg te schrijven
using Microsoft.Maui.Storage;
// Alias zodat we korter kunnen verwijzen naar de entity classes (Boek, Lid, Lenen, Categorie)
using Entiteiten = Biblio_Models.Entiteiten;
using Biblio_App.Models.Pagination;

namespace Biblio_App.Services
{
    public interface IDataSyncService
    {
        // Haalt alle data op van de API en slaat die lokaal op (een “sync alles” knop/actie)
        Task SyncAllAsync();

        // Boeken - preferApi default = false (offline-first)
        Task<List<Entiteiten.Boek>> GetBoekenAsync(bool preferApi = false);
        Task<Entiteiten.Boek?> CreateBoekAsync(Entiteiten.Boek model);
        Task<bool> UpdateBoekAsync(Entiteiten.Boek model);
        Task<bool> DeleteBoekAsync(int id);

        // Leden - preferApi default = false (offline-first)
        Task<List<Entiteiten.Lid>> GetLedenAsync(bool preferApi = false);
        Task<Entiteiten.Lid?> CreateLidAsync(Entiteiten.Lid model);
        Task<bool> UpdateLidAsync(Entiteiten.Lid model);
        Task<bool> DeleteLidAsync(int id);

        // Uitleningen - preferApi default = false (offline-first)
        Task<List<Entiteiten.Lenen>> GetUitleningenAsync(bool preferApi = false);
        Task<Entiteiten.Lenen?> CreateUitleningAsync(Entiteiten.Lenen model);
        Task<bool> UpdateUitleningAsync(Entiteiten.Lenen model);
        Task<bool> DeleteUitleningAsync(int id);

        // Categorien - preferApi default = false (offline-first)
        Task<List<Entiteiten.Categorie>> GetCategorieenAsync(bool preferApi = false);
        Task<Entiteiten.Categorie?> CreateCategorieAsync(Entiteiten.Categorie model);
        Task<bool> UpdateCategorieAsync(Entiteiten.Categorie model);
        Task<bool> DeleteCategorieAsync(int id);
    }

    public class DataSyncService : IDataSyncService
    {
        // Factory om HttpClient instances op te halen (geconfigureerd in `MauiProgram.cs`, incl. token/headers)
        private readonly IHttpClientFactory _httpFactory;

        // Lokale opslaglaag (SQLite/EF Core of andere lokale DB) voor offline-first gebruik
        private readonly ILocalRepository _local;

        // Single timeout used across API calls. Keep HttpClient.Timeout separate
        // when using an explicit CancellationTokenSource in long-running flows.
        private static readonly TimeSpan ApiRequestTimeout = TimeSpan.FromMinutes(2);

        public DataSyncService(IHttpClientFactory httpFactory, ILocalRepository local)
        {
            // Dependency injection: services worden via de DI container doorgegeven
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        private void LogMarker(string message)
        {
            // Kleine helper om debug-info weg te schrijven naar een file.
            // Handig bij testen op device/emulator wanneer je geen console output ziet.
            try
            {
                // FileSystem.AppDataDirectory is platform-specific. Suppress analyzer here
                // because this MAUI app targets multiple platforms where the API is supported.
#pragma warning disable CA1416 // Validate platform compatibility
                var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] {message}\n");
#pragma warning restore CA1416
            }
            catch { }
        }

        public async Task SyncAllAsync()
        {
            // 1) Maak een HttpClient die al auth (token) bevat.
            var client = _httpFactory.CreateClient("ApiWithToken");
            // Use an explicit CancellationTokenSource for timeout control and avoid
            // HttpClient's built-in timeout canceling internal operations. The
            // CancellationTokenSource (cts) below will control request timeouts.

            using var cts = new CancellationTokenSource(ApiRequestTimeout);

            // 2) Boeken: haal paged lijst via API en bewaar lokaal.
            // We gebruiken try/catch zodat de app niet crasht als de API onbereikbaar is.
            // Bij een fout loggen we de fout en gaan we verder met de volgende dataset.
            try
            {
                var remoteBoeken = await client.GetFromJsonAsync<PagedResult<Entiteiten.Boek>>("api/boeken?page=1&pageSize=1000", cancellationToken: cts.Token);
                if (remoteBoeken?.Items != null)
                {
                    await _local.SaveBoekenAsync(remoteBoeken.Items);
                }
            }
            catch (TaskCanceledException tce)
            {
                // TaskCanceledException = meestal timeout (request duurt te lang) of annulering via de token.
                LogMarker($"SyncAll boeken canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Algemene fout (bv. geen internet, server error, JSON parse error, ...)
                LogMarker($"SyncAll boeken error: {ex}");
            }

            // 3) Leden: haal paged lijst via API en bewaar lokaal.
            // Opnieuw try/catch per onderdeel: zo blijft de sync robuust.
            try
            {
                var remoteLeden = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lid>>("api/leden?page=1&pageSize=1000", cancellationToken: cts.Token);
                if (remoteLeden?.Items != null)
                {
                    foreach (var l in remoteLeden.Items) await _local.SaveLidAsync(l);
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering tijdens het ophalen van leden
                LogMarker($"SyncAll leden canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Overige fouten tijdens het ophalen van leden
                LogMarker($"SyncAll leden error: {ex}");
            }

            // 4) Uitleningen: haal paged lijst via API en bewaar lokaal.
            // Elke API-call staat apart in try/catch zodat één probleem niet alles blokkeert.
            try
            {
                var remoteUit = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lenen>>("api/uitleningen?page=1&pageSize=1000", cancellationToken: cts.Token);
                if (remoteUit?.Items != null)
                {
                    foreach (var u in remoteUit.Items) await _local.SaveUitleningAsync(u);
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering tijdens create/read/update/delete is niet van toepassing hier; dit is enkel read.
                LogMarker($"SyncAll uitleningen canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"SyncAll uitleningen error: {ex}");
            }

            // 5) Categorieën: in dit endpoint komt een eenvoudige lijst terug (geen paging).
            // Ook hier: try/catch zodat een probleem met categorieën de app niet laat crashen.
            try
            {
                var remoteCat = await client.GetFromJsonAsync<List<Entiteiten.Categorie>>("api/categorieen", cancellationToken: cts.Token);
                if (remoteCat != null)
                {
                    await _local.SaveCategorieenAsync(remoteCat);
                }
            }
            catch (TaskCanceledException tce)
            {
                LogMarker($"SyncAll categorieen canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"SyncAll categorieen error: {ex}");
            }

            LogMarker("SyncAll completed.");
        }

        // -------------------------
        // BOEKEN (offline-first)
        // -------------------------
        public async Task<List<Entiteiten.Boek>> GetBoekenAsync(bool preferApi = false)
        {
            // preferApi = true => eerst de API proberen (online), daarna lokaal opslaan.
            // preferApi = false => enkel lokaal lezen (sneller + werkt offline).
            if (preferApi)
            {
                // We proberen online data op te halen.
                // Als dat faalt (timeout/geen internet), dan vallen we hieronder terug op lokale data.
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Boek>>("api/boeken?page=1&pageSize=1000", cts.Token);
                    var items = (resp?.Items ?? Array.Empty<Entiteiten.Boek>()).ToList();

                    // Lokaal cachen zodat de app daarna offline ook dezelfde data gebruikt
                    await _local.SaveBoekenAsync(items);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    // Timeout/annulering tijdens het ophalen
                    LogMarker($"GetBoekenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    // Elke andere fout (netwerk, server, JSON, ...)
                    LogMarker($"GetBoekenAsync error: {ex}");
                }
            }

            // Fallback (of offline modus): lees uit de lokale database
            return await _local.GetBoekenAsync();
        }

        public async Task<Entiteiten.Boek?> CreateBoekAsync(Entiteiten.Boek model)
        {
            // Nieuw boek aanmaken: eerst proberen via API. Lukt dat niet, dan lokaal opslaan.
            try
            {
                // Probeer online aan te maken, zodat de server de officiële Id kan genereren.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PostAsJsonAsync("api/boeken", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Entiteiten.Boek>();
                    if (created != null) await _local.SaveBoekAsync(created);
                    return created;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering: we loggen dit en gaan offline verder.
                LogMarker($"CreateBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout: we loggen dit en gaan offline verder.
                LogMarker($"CreateBoekAsync error: {ex}");
            }

            // Fallback local: ook als de API niet werkt, kan de gebruiker doorgaan.
            await _local.SaveBoekAsync(model);
            return model;
        }

        public async Task<bool> UpdateBoekAsync(Entiteiten.Boek model)
        {
            // Boek aanpassen: API update + lokale update.
            // Bij fout: we updaten toch lokaal zodat de gebruiker offline verder kan.
            try
            {
                // Probeer eerst server-side te updaten zodat alles gesynchroniseerd blijft.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PutAsJsonAsync($"api/boeken/{model.Id}", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveBoekAsync(model);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering: update lokaal zodat de UI niet geblokkeerd is.
                LogMarker($"UpdateBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout: update lokaal zodat de gebruiker offline kan verder werken.
                LogMarker($"UpdateBoekAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveBoekAsync(model);
            return true;
        }

        public async Task<bool> DeleteBoekAsync(int id)
        {
            // Boek verwijderen: probeer eerst via API, daarna ook lokaal verwijderen.
            try
            {
                // Probeer op de server te verwijderen. Bij fout verwijderen we het lokaal toch.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.DeleteAsync($"api/boeken/{id}", cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteBoekAsync(id);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering: lokaal verwijderen zodat de app consistent blijft.
                LogMarker($"DeleteBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout: lokaal verwijderen zodat de gebruiker offline kan doorgaan.
                LogMarker($"DeleteBoekAsync error: {ex}");
            }

            // Fallback local
            await _local.DeleteBoekAsync(id);
            return true;
        }

        // -------------------------
        // LEDEN (offline-first)
        // -------------------------
        public async Task<List<Entiteiten.Lid>> GetLedenAsync(bool preferApi = false)
        {
            // Zelfde patroon als boeken: optioneel API lezen en lokaal cachen.
            if (preferApi)
            {
                // We proberen de API. Als dit mislukt, vallen we terug op lokale data.
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lid>>("api/leden?page=1&pageSize=1000", cts.Token);
                    var items = (resp?.Items ?? Array.Empty<Entiteiten.Lid>()).ToList();
                    foreach (var l in items) await _local.SaveLidAsync(l);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    // Timeout/annulering
                    LogMarker($"GetLedenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    // Andere fout
                    LogMarker($"GetLedenAsync error: {ex}");
                }
            }

            // Fallback: lokale data
            return await _local.GetLedenAsync();
        }

        public async Task<Entiteiten.Lid?> CreateLidAsync(Entiteiten.Lid model)
        {
            // Lid aanmaken: API proberen (online), anders lokaal (offline fallback).
            try
            {
                // Probeer online: zo krijgen we een server-side Id terug.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PostAsJsonAsync("api/leden", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Entiteiten.Lid>();
                    if (created != null) await _local.SaveLidAsync(created);
                    return created;
                }

                LogMarker($"CreateLidAsync API returned status: {(int)res.StatusCode} {res.ReasonPhrase}");
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering: we gaan offline verder.
                LogMarker($"CreateLidAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout: we gaan offline verder.
                LogMarker($"CreateLidAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveLidAsync(model);
            return model;
        }

        public async Task<bool> UpdateLidAsync(Entiteiten.Lid model)
        {
            // Lid aanpassen: API update met timeout + lokale update.
            try
            {
                // Probeer eerst API update.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PutAsJsonAsync($"api/leden/{model.Id}", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveLidAsync(model);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"UpdateLidAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                 // Andere fout
                LogMarker($"UpdateLidAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveLidAsync(model);
            return true;
        }

        public async Task<bool> DeleteLidAsync(int id)
        {
            // Lid verwijderen: API delete + lokale delete.
            try
            {
                // Probeer server-side delete.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.DeleteAsync($"api/leden/{id}", cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteLidAsync(id);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"DeleteLidAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"DeleteLidAsync error: {ex}");
            }

            // Fallback local
            await _local.DeleteLidAsync(id);
            return true;
        }

        // -------------------------
        // UITLENINGEN (offline-first)
        // -------------------------
        public async Task<List<Entiteiten.Lenen>> GetUitleningenAsync(bool preferApi = false)
        {
            // Uitleningen ophalen met eventueel API voorkeur.
            if (preferApi)
            {
                // Probeer online. Bij fout -> lokaal.
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lenen>>("api/uitleningen?page=1&pageSize=1000", cts.Token);
                    var items = (resp?.Items ?? Array.Empty<Entiteiten.Lenen>()).ToList();
                    foreach (var u in items) await _local.SaveUitleningAsync(u);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    // Timeout/annulering
                    LogMarker($"GetUitleningenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    // Andere fout
                    LogMarker($"GetUitleningenAsync error: {ex}");
                }
            }

            // Fallback local
            return await _local.GetUitleningenAsync();
        }

        public async Task<Entiteiten.Lenen?> CreateUitleningAsync(Entiteiten.Lenen model)
        {
            // Nieuwe uitlening aanmaken: API proberen, anders lokaal.
            try
            {
                // Probeer online aan te maken.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PostAsJsonAsync("api/uitleningen", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Entiteiten.Lenen>();
                    if (created != null) await _local.SaveUitleningAsync(created);
                    return created;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"CreateUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"CreateUitleningAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveUitleningAsync(model);
            return model;
        }

        public async Task<bool> UpdateUitleningAsync(Entiteiten.Lenen model)
        {
            // Uitlening aanpassen: API + lokale update.
            try
            {
                // Probeer API update.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PutAsJsonAsync($"api/uitleningen/{model.Id}", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveUitleningAsync(model);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"UpdateUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"UpdateUitleningAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveUitleningAsync(model);
            return true;
        }

        public async Task<bool> DeleteUitleningAsync(int id)
        {
            // Uitlening verwijderen: API + lokaal.
            try
            {
                // Probeer API delete.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.DeleteAsync($"api/uitleningen/{id}", cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteUitleningAsync(id);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"DeleteUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"DeleteUitleningAsync error: {ex}");
            }

            // Fallback local
            await _local.DeleteUitleningAsync(id);
            return true;
        }

        // -------------------------
        // CATEGORIEËN (offline-first)
        // -------------------------
        public async Task<List<Entiteiten.Categorie>> GetCategorieenAsync(bool preferApi = false)
        {
            // Categorieën ophalen. API geeft een lijst terug (geen paging).
            if (preferApi)
            {
                // Probeer online. Bij fout -> lokale data.
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<List<Entiteiten.Categorie>>("api/categorieen", cts.Token);
                    var items = resp ?? new List<Entiteiten.Categorie>();
                    if (items.Any()) await _local.SaveCategorieenAsync(items);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    // Timeout/annulering
                    LogMarker($"GetCategorieenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    // Andere fout
                    LogMarker($"GetCategorieenAsync error: {ex}");
                }
            }

            // Fallback local
            return await _local.GetCategorieenAsync();
        }

        public async Task<Entiteiten.Categorie?> CreateCategorieAsync(Entiteiten.Categorie model)
        {
            // Nieuwe categorie aanmaken: API proberen, anders lokaal.
            try
            {
                // Probeer online aan te maken.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PostAsJsonAsync("api/categorieen", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Entiteiten.Categorie>();
                    if (created != null) await _local.SaveCategorieAsync(created);
                    return created;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"CreateCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"CreateCategorieAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveCategorieAsync(model);
            return model;
        }

        public async Task<bool> UpdateCategorieAsync(Entiteiten.Categorie model)
        {
            // Categorie aanpassen: API update + lokale update.
            try
            {
                // Probeer API update.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.PutAsJsonAsync($"api/categorieen/{model.Id}", model, cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveCategorieAsync(model);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"UpdateCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"UpdateCategorieAsync error: {ex}");
            }

            // Fallback local
            await _local.SaveCategorieAsync(model);
            return true;
        }

        public async Task<bool> DeleteCategorieAsync(int id)
        {
            // Categorie verwijderen: API delete + lokale delete.
            try
            {
                // Probeer API delete.
                var client = _httpFactory.CreateClient("ApiWithToken");
                using var cts = new CancellationTokenSource(ApiRequestTimeout);
                var res = await client.DeleteAsync($"api/categorieen/{id}", cts.Token);
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteCategorieAsync(id);
                    return true;
                }
            }
            catch (TaskCanceledException tce)
            {
                // Timeout/annulering
                LogMarker($"DeleteCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                // Andere fout
                LogMarker($"DeleteCategorieAsync error: {ex}");
            }

            // Fallback local
            await _local.DeleteCategorieAsync(id);
            return true;
        }
    }
}
