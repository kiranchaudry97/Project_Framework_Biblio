using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using Biblio_Models.Entiteiten;
using Biblio_App.Models.Pagination;
using Biblio_Models.Entiteiten;
using System.IO;
using Microsoft.Maui.Storage;

namespace Biblio_App.Services
{
    public interface IDataSyncService
    {
        Task SyncAllAsync();

        // Boeken
        Task<List<Boek>> GetBoekenAsync(bool preferApi = true);
        Task<Boek?> CreateBoekAsync(Boek model);
        Task<bool> UpdateBoekAsync(Boek model);
        Task<bool> DeleteBoekAsync(int id);

        // Leden
        Task<List<Lid>> GetLedenAsync(bool preferApi = true);
        Task<Lid?> CreateLidAsync(Lid model);
        Task<bool> UpdateLidAsync(Lid model);
        Task<bool> DeleteLidAsync(int id);

        // Uitleningen
        Task<List<Lenen>> GetUitleningenAsync(bool preferApi = true);
        Task<Lenen?> CreateUitleningAsync(Lenen model);
        Task<bool> UpdateUitleningAsync(Lenen model);
        Task<bool> DeleteUitleningAsync(int id);

        // Categorien
        Task<List<Categorie>> GetCategorieenAsync(bool preferApi = true);
        Task<Categorie?> CreateCategorieAsync(Categorie model);
        Task<bool> UpdateCategorieAsync(Categorie model);
        Task<bool> DeleteCategorieAsync(int id);
    }

    public class DataSyncService : IDataSyncService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILocalRepository _local;

        public DataSyncService(IHttpClientFactory httpFactory, ILocalRepository local)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        private void LogMarker(string message)
        {
            try
            {
                var marker = Path.Combine(FileSystem.AppDataDirectory, "biblio_seed.log");
                File.AppendAllText(marker, $"[{DateTime.UtcNow:o}] {message}\n");
            }
            catch { }
        }

        public async Task SyncAllAsync()
        {
            var client = _httpFactory.CreateClient("ApiWithToken");

            // Probeer boeken op te halen
            try
            {
                var remoteBoeken = await client.GetFromJsonAsync<PagedResult<Boek>>("api/boeken?page=1&pageSize=1000");
                if (remoteBoeken?.Items != null)
                {
                    await _local.SaveBoekenAsync(remoteBoeken.Items);
                }
            }
            catch (Exception ex) { LogMarker($"SyncAll boeken error: {ex}"); }

            // Leden
            try
            {
                var remoteLeden = await client.GetFromJsonAsync<PagedResult<Lid>>("api/leden?page=1&pageSize=1000");
                if (remoteLeden?.Items != null)
                {
                    foreach (var l in remoteLeden.Items) await _local.SaveLidAsync(l);
                }
            }
            catch (Exception ex) { LogMarker($"SyncAll leden error: {ex}"); }

            // Uitleningen
            try
            {
                var remoteUit = await client.GetFromJsonAsync<PagedResult<Lenen>>("api/uitleningen?page=1&pageSize=1000");
                if (remoteUit?.Items != null)
                {
                    foreach (var u in remoteUit.Items) await _local.SaveUitleningAsync(u);
                }
            }
            catch (Exception ex) { LogMarker($"SyncAll uitleningen error: {ex}"); }

            // Categorieen
            try
            {
                var remoteCat = await client.GetFromJsonAsync<List<Categorie>>("api/categorieen");
                if (remoteCat != null)
                {
                    await _local.SaveCategorieenAsync(remoteCat);
                }
            }
            catch (Exception ex) { LogMarker($"SyncAll categorieen error: {ex}"); }

            LogMarker("SyncAll completed.");
        }

        // Boeken
        public async Task<List<Boek>> GetBoekenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    var resp = await client.GetFromJsonAsync<PagedResult<Boek>>("api/boeken?page=1&pageSize=1000");
                    var items = resp?.Items ?? new List<Boek>();
                    await _local.SaveBoekenAsync(items);
                    return items;
                }
                catch
                {
                    // fallback
                }
            }
            return await _local.GetBoekenAsync();
        }

        public async Task<Boek?> CreateBoekAsync(Boek model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PostAsJsonAsync("api/boeken", model);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Boek>();
                    if (created != null) await _local.SaveBoekAsync(created);
                    return created;
                }
            }
            catch { }

            // fallback local
            await _local.SaveBoekAsync(model);
            return model;
        }

        public async Task<bool> UpdateBoekAsync(Boek model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PutAsJsonAsync($"api/boeken/{model.Id}", model);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveBoekAsync(model);
                    return true;
                }
            }
            catch { }

            // fallback local
            await _local.SaveBoekAsync(model);
            return true;
        }

        public async Task<bool> DeleteBoekAsync(int id)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.DeleteAsync($"api/boeken/{id}");
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteBoekAsync(id);
                    return true;
                }
            }
            catch { }

            await _local.DeleteBoekAsync(id);
            return true;
        }

        // Leden
        public async Task<List<Lid>> GetLedenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    var resp = await client.GetFromJsonAsync<PagedResult<Lid>>("api/leden?page=1&pageSize=1000");
                    var items = resp?.Items ?? new List<Lid>();
                    foreach (var l in items) await _local.SaveLidAsync(l);
                    return items;
                }
                catch { }
            }
            return await _local.GetLedenAsync();
        }

        public async Task<Lid?> CreateLidAsync(Lid model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PostAsJsonAsync("api/leden", model);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Lid>();
                    if (created != null) await _local.SaveLidAsync(created);
                    return created;
                }
            }
            catch { }

            await _local.SaveLidAsync(model);
            return model;
        }

        public async Task<bool> UpdateLidAsync(Lid model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PutAsJsonAsync($"api/leden/{model.Id}", model);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveLidAsync(model);
                    return true;
                }
            }
            catch { }

            await _local.SaveLidAsync(model);
            return true;
        }

        public async Task<bool> DeleteLidAsync(int id)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.DeleteAsync($"api/leden/{id}");
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteLidAsync(id);
                    return true;
                }
            }
            catch { }

            await _local.DeleteLidAsync(id);
            return true;
        }

        // Uitleningen
        public async Task<List<Lenen>> GetUitleningenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    var resp = await client.GetFromJsonAsync<PagedResult<Lenen>>("api/uitleningen?page=1&pageSize=1000");
                    var items = resp?.Items ?? new List<Lenen>();
                    foreach (var u in items) await _local.SaveUitleningAsync(u);
                    return items;
                }
                catch { }
            }
            return await _local.GetUitleningenAsync();
        }

        public async Task<Lenen?> CreateUitleningAsync(Lenen model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PostAsJsonAsync("api/uitleningen", model);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Lenen>();
                    if (created != null) await _local.SaveUitleningAsync(created);
                    return created;
                }
            }
            catch { }

            await _local.SaveUitleningAsync(model);
            return model;
        }

        public async Task<bool> UpdateUitleningAsync(Lenen model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PutAsJsonAsync($"api/uitleningen/{model.Id}", model);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveUitleningAsync(model);
                    return true;
                }
            }
            catch { }

            await _local.SaveUitleningAsync(model);
            return true;
        }

        public async Task<bool> DeleteUitleningAsync(int id)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.DeleteAsync($"api/uitleningen/{id}");
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteUitleningAsync(id);
                    return true;
                }
            }
            catch { }

            await _local.DeleteUitleningAsync(id);
            return true;
        }

        // Categoriën
        public async Task<List<Categorie>> GetCategorieenAsync(bool preferApi = true)
        {
            if (preferApi)
            {
                try
                {
                    var client = _httpFactory.CreateClient("ApiWithToken");
                    var resp = await client.GetFromJsonAsync<List<Categorie>>("api/categorieen");
                    var items = resp ?? new List<Categorie>();
                    if (items.Any()) await _local.SaveCategorieenAsync(items);
                    return items;
                }
                catch
                {
                    // fallback
                }
            }
            return await _local.GetCategorieenAsync();
        }

        public async Task<Categorie?> CreateCategorieAsync(Categorie model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PostAsJsonAsync("api/categorieen", model);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<Categorie>();
                    if (created != null) await _local.SaveCategorieAsync(created);
                    return created;
                }
            }
            catch { }

            await _local.SaveCategorieAsync(model);
            return model;
        }

        public async Task<bool> UpdateCategorieAsync(Categorie model)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.PutAsJsonAsync($"api/categorieen/{model.Id}", model);
                if (res.IsSuccessStatusCode)
                {
                    await _local.SaveCategorieAsync(model);
                    return true;
                }
            }
            catch { }

            await _local.SaveCategorieAsync(model);
            return true;
        }

        public async Task<bool> DeleteCategorieAsync(int id)
        {
            try
            {
                var client = _httpFactory.CreateClient("ApiWithToken");
                var res = await client.DeleteAsync($"api/categorieen/{id}");
                if (res.IsSuccessStatusCode)
                {
                    await _local.DeleteCategorieAsync(id);
                    return true;
                }
            }
            catch { }

            await _local.DeleteCategorieAsync(id);
            return true;
        }
    }
}
