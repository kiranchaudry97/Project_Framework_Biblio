using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using Entiteiten = Biblio_Models.Entiteiten;
using Biblio_App.Models.Pagination;
using System.IO;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
    public interface IDataSyncService
    {
        Task SyncAllAsync();

        // Boeken
        Task<List<Entiteiten.Boek>> GetBoekenAsync(bool preferApi = true);
        Task<Entiteiten.Boek?> CreateBoekAsync(Entiteiten.Boek model);
        Task<bool> UpdateBoekAsync(Entiteiten.Boek model);
        Task<bool> DeleteBoekAsync(int id);

        // Leden
        Task<List<Entiteiten.Lid>> GetLedenAsync(bool preferApi = true);
        Task<Entiteiten.Lid?> CreateLidAsync(Entiteiten.Lid model);
        Task<bool> UpdateLidAsync(Entiteiten.Lid model);
        Task<bool> DeleteLidAsync(int id);

        // Uitleningen
        Task<List<Entiteiten.Lenen>> GetUitleningenAsync(bool preferApi = true);
        Task<Entiteiten.Lenen?> CreateUitleningAsync(Entiteiten.Lenen model);
        Task<bool> UpdateUitleningAsync(Entiteiten.Lenen model);
        Task<bool> DeleteUitleningAsync(int id);

        // Categorien
        Task<List<Entiteiten.Categorie>> GetCategorieenAsync(bool preferApi = true);
        Task<Entiteiten.Categorie?> CreateCategorieAsync(Entiteiten.Categorie model);
        Task<bool> UpdateCategorieAsync(Entiteiten.Categorie model);
        Task<bool> DeleteCategorieAsync(int id);
    }

    public class DataSyncService : IDataSyncService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILocalRepository _local;

        // Single timeout used across API calls. Keep HttpClient.Timeout separate
        // when using an explicit CancellationTokenSource in long-running flows.
        private static readonly TimeSpan ApiRequestTimeout = TimeSpan.FromMinutes(2);

        public DataSyncService(IHttpClientFactory httpFactory, ILocalRepository local)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        private void LogMarker(string message)
        {
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
            var client = _httpFactory.CreateClient("ApiWithToken");
            // Use an explicit CancellationTokenSource for timeout control and avoid
            // HttpClient's built-in timeout canceling internal operations. The
            // CancellationTokenSource (cts) below will control request timeouts.

            using var cts = new CancellationTokenSource(ApiRequestTimeout);

            // Probeer boeken op te halen
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
                LogMarker($"SyncAll boeken canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"SyncAll boeken error: {ex}");
            }

            // Leden
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
                LogMarker($"SyncAll leden canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"SyncAll leden error: {ex}");
            }

            // Uitleningen
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
                LogMarker($"SyncAll uitleningen canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"SyncAll uitleningen error: {ex}");
            }

            // Categorieen
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

        // Boeken
        public async Task<List<Entiteiten.Boek>> GetBoekenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Boek>>("api/boeken?page=1&pageSize=1000", cts.Token);
                    var items = resp?.Items ?? new List<Entiteiten.Boek>();
                    await _local.SaveBoekenAsync(items);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    LogMarker($"GetBoekenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    LogMarker($"GetBoekenAsync error: {ex}");
                }
            }
            return await _local.GetBoekenAsync();
        }

        public async Task<Entiteiten.Boek?> CreateBoekAsync(Entiteiten.Boek model)
        {
            try
            {
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
                LogMarker($"CreateBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"CreateBoekAsync error: {ex}");
            }

            // fallback local
            await _local.SaveBoekAsync(model);
            return model;
        }

        public async Task<bool> UpdateBoekAsync(Entiteiten.Boek model)
        {
            try
            {
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
                LogMarker($"UpdateBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"UpdateBoekAsync error: {ex}");
            }

            // fallback local
            await _local.SaveBoekAsync(model);
            return true;
        }

        public async Task<bool> DeleteBoekAsync(int id)
        {
            try
            {
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
                LogMarker($"DeleteBoekAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"DeleteBoekAsync error: {ex}");
            }

            await _local.DeleteBoekAsync(id);
            return true;
        }

        // Leden
        public async Task<List<Entiteiten.Lid>> GetLedenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lid>>("api/leden?page=1&pageSize=1000", cts.Token);
                    var items = resp?.Items ?? new List<Entiteiten.Lid>();
                    foreach (var l in items) await _local.SaveLidAsync(l);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    LogMarker($"GetLedenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    LogMarker($"GetLedenAsync error: {ex}");
                }
            }
            return await _local.GetLedenAsync();
        }

        public async Task<Entiteiten.Lid?> CreateLidAsync(Entiteiten.Lid model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PostAsJsonAsync("api/leden", model);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Entiteiten.Lid>();
                    if (created != null) await _local.SaveLidAsync(created);
                    return created;
                }
            }
            catch { }

            await _local.SaveLidAsync(model);
            return model;
        }

        public async Task<bool> UpdateLidAsync(Entiteiten.Lid model)
        {
            try
            {
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
                LogMarker($"UpdateLidAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"UpdateLidAsync error: {ex}");
            }

            await _local.SaveLidAsync(model);
            return true;
        }

        public async Task<bool> DeleteLidAsync(int id)
        {
            try
            {
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
                LogMarker($"DeleteLidAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"DeleteLidAsync error: {ex}");
            }

            await _local.DeleteLidAsync(id);
            return true;
        }

        // Uitleningen
        public async Task<List<Entiteiten.Lenen>> GetUitleningenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    using var cts = new CancellationTokenSource(ApiRequestTimeout);
                    var resp = await client.GetFromJsonAsync<PagedResult<Entiteiten.Lenen>>("api/uitleningen?page=1&pageSize=1000", cts.Token);
                    var items = resp?.Items ?? new List<Entiteiten.Lenen>();
                    foreach (var u in items) await _local.SaveUitleningAsync(u);
                    return items;
                }
                catch (TaskCanceledException tce)
                {
                    LogMarker($"GetUitleningenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    LogMarker($"GetUitleningenAsync error: {ex}");
                }
            }
            return await _local.GetUitleningenAsync();
        }

        public async Task<Entiteiten.Lenen?> CreateUitleningAsync(Entiteiten.Lenen model)
        {
            try
            {
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
                LogMarker($"CreateUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"CreateUitleningAsync error: {ex}");
            }

            await _local.SaveUitleningAsync(model);
            return model;
        }

        public async Task<bool> UpdateUitleningAsync(Entiteiten.Lenen model)
        {
            try
            {
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
                LogMarker($"UpdateUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"UpdateUitleningAsync error: {ex}");
            }

            await _local.SaveUitleningAsync(model);
            return true;
        }

        public async Task<bool> DeleteUitleningAsync(int id)
        {
            try
            {
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
                LogMarker($"DeleteUitleningAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"DeleteUitleningAsync error: {ex}");
            }

            await _local.DeleteUitleningAsync(id);
            return true;
        }

        // Categoriën
        public async Task<List<Entiteiten.Categorie>> GetCategorieenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
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
                    LogMarker($"GetCategorieenAsync canceled: {tce.Message}");
                }
                catch (Exception ex)
                {
                    LogMarker($"GetCategorieenAsync error: {ex}");
                }
            }
            return await _local.GetCategorieenAsync();
        }

        public async Task<Entiteiten.Categorie?> CreateCategorieAsync(Entiteiten.Categorie model)
        {
            try
            {
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
                LogMarker($"CreateCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"CreateCategorieAsync error: {ex}");
            }

            await _local.SaveCategorieAsync(model);
            return model;
        }

        public async Task<bool> UpdateCategorieAsync(Entiteiten.Categorie model)
        {
            try
            {
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
                LogMarker($"UpdateCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"UpdateCategorieAsync error: {ex}");
            }

            await _local.SaveCategorieAsync(model);
            return true;
        }

        public async Task<bool> DeleteCategorieAsync(int id)
        {
            try
            {
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
                LogMarker($"DeleteCategorieAsync canceled: {tce.Message}");
            }
            catch (Exception ex)
            {
                LogMarker($"DeleteCategorieAsync error: {ex}");
            }

            await _local.DeleteCategorieAsync(id);
            return true;
        }
    }
}
