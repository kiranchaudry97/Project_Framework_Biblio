using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;

namespace Biblio_App
{
    internal class Synchronizer
    {
        HttpClient client;
        JsonSerializerOptions sOptions;
        internal bool dbExists = false;

        readonly IDbContextFactory<Biblio_Models.Data.LocalDbContext> _dbFactory;
        readonly string? _apiBase;

        // Optional current user state kept locally in the synchronizer
        internal AppUser? CurrentUser { get; private set; }
        internal string? CurrentUserId { get; private set; }

        internal Synchronizer(IDbContextFactory<Biblio_Models.Data.LocalDbContext> dbFactory, string? apiBase = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _apiBase = apiBase;

            client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_apiBase))
            {
                try { client.BaseAddress = new Uri(_apiBase); } catch { /* ignore */ }
            }

            sOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
        }

        // New ctor that accepts an HttpClient (e.g. created via IHttpClientFactory.CreateClient("ApiWithToken"))
        internal Synchronizer(IDbContextFactory<Biblio_Models.Data.LocalDbContext> dbFactory, HttpClient httpClient, string? apiBase = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _apiBase = apiBase;

            client = httpClient ?? new HttpClient();
            // Ensure BaseAddress when not already set on the provided client
            if (client.BaseAddress == null && !string.IsNullOrWhiteSpace(_apiBase))
            {
                try { client.BaseAddress = new Uri(_apiBase); } catch { }
            }

            sOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
        }

        // Synchronize books from remote to local (best-effort). Does not remove local-only changes.
        async Task AllBooks()
        {
            // Synchronize local changes to API: not implemented here

            // Synchronize from API to local if authorized and API configured
            if (await IsAuthorized() && client.BaseAddress != null)
            {
                try
                {
                    var response = await client.GetAsync("api/mobiledata");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<MobileDataDto>(responseBody, sOptions);
                    if (data != null)
                    {
                        using var ctx = _dbFactory.CreateDbContext();

                        // Upsert categories
                        if (data.Categories != null)
                        {
                            foreach (var c in data.Categories)
                            {
                                var existingC = await ctx.Categorien.FirstOrDefaultAsync(x => x.Id == c.Id);
                                if (existingC != null)
                                {
                                    existingC.Naam = c.Naam ?? existingC.Naam;
                                    existingC.IsDeleted = false;
                                }
                                else
                                {
                                    ctx.Categorien.Add(new Categorie { Id = c.Id, Naam = c.Naam ?? string.Empty });
                                }
                            }
                        }

                        // Upsert books
                        if (data.Books != null)
                        {
                            foreach (var b in data.Books)
                            {
                                var existing = await ctx.Boeken.FirstOrDefaultAsync(x => x.Id == b.Id);
                                if (existing != null)
                                {
                                    existing.Titel = b.Titel ?? existing.Titel;
                                    existing.Auteur = b.Auteur ?? existing.Auteur;
                                    existing.Isbn = b.Isbn ?? existing.Isbn;
                                    existing.CategorieID = b.CategorieId;
                                    existing.IsDeleted = false;
                                }
                                else
                                {
                                    ctx.Boeken.Add(new Boek
                                    {
                                        Id = b.Id,
                                        Titel = b.Titel ?? string.Empty,
                                        Auteur = b.Auteur ?? string.Empty,
                                        Isbn = b.Isbn ?? string.Empty,
                                        CategorieID = b.CategorieId
                                    });
                                }
                            }
                        }

                        // Upsert members
                        if (data.Members != null)
                        {
                            foreach (var m in data.Members)
                            {
                                var existingM = await ctx.Leden.FirstOrDefaultAsync(x => x.Id == m.Id);
                                if (existingM != null)
                                {
                                    existingM.Voornaam = m.Voornaam ?? existingM.Voornaam;
                                    existingM.AchterNaam = m.AchterNaam ?? existingM.AchterNaam;
                                    existingM.Email = m.Email ?? existingM.Email;
                                    existingM.IsDeleted = false;
                                }
                                else
                                {
                                    ctx.Leden.Add(new Lid
                                    {
                                        Id = m.Id,
                                        Voornaam = m.Voornaam ?? string.Empty,
                                        AchterNaam = m.AchterNaam ?? string.Empty,
                                        Email = m.Email
                                    });
                                }
                            }
                        }

                        await ctx.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // best-effort: ignore failures
                }
            }
        }

        internal async Task<bool> IsAuthorized()
        {
            if (!string.IsNullOrEmpty(CurrentUserId))
                return true;

            if (client.BaseAddress == null)
                return false;

            try
            {
                var resp = await client.GetAsync("api/auth/isauthorized");
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<AppUser>(body, sOptions);
                if (user != null)
                {
                    CurrentUser = user;
                    CurrentUserId = user.Id;

                    try { Preferences.Default.Set("CurrentUserId", CurrentUserId ?? string.Empty); } catch { }

                    return true;
                }
            }
            catch { }

            return false;
        }

        internal async Task InitializeDb()
        {
            try
            {
                using var ctx = _dbFactory.CreateDbContext();

                await ctx.Database.MigrateAsync();

                if (!await ctx.Categorien.AnyAsync())
                {
                    ctx.Categorien.AddRange(
                        new Categorie { Naam = "Roman" },
                        new Categorie { Naam = "Jeugd" },
                        new Categorie { Naam = "Thriller" },
                        new Categorie { Naam = "Wetenschap" }
                    );
                    await ctx.SaveChangesAsync();
                }

                if (!await ctx.Boeken.AnyAsync())
                {
                    var roman = await ctx.Categorien.FirstAsync(c => c.Naam == "Roman");
                    var jeugd = await ctx.Categorien.FirstAsync(c => c.Naam == "Jeugd");

                    ctx.Boeken.AddRange(
                        new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                        new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                        new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id }
                    );
                    await ctx.SaveChangesAsync();
                }

                dbExists = true;
            }
            catch (Exception)
            {
            }
        }

        internal async Task<bool> Login(LoginModel loginModel)
        {
            if (client.BaseAddress == null)
                return false; // no API configured

            try
            {
                var jsonString = JsonSerializer.Serialize(loginModel, sOptions);
                var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/auth/login", content);
                if (!response.IsSuccessStatusCode)
                    return false;

                var respBody = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<AppUser>(respBody, sOptions);
                if (user != null)
                {
                    CurrentUser = user;
                    CurrentUserId = user.Id;

                    // persist minimal info in preferences
                    try { Preferences.Default.Set("CurrentUserId", CurrentUserId ?? string.Empty); } catch { }
                    try { Preferences.Default.Set("CurrentUserJson", JsonSerializer.Serialize(user, sOptions)); } catch { }

                    return true;
                }
            }
            catch { }

            return false;
        }

        private async Task LoginToAPI()
        {
            if (await IsAuthorized())
                return;

            try
            {
                var savedUserJson = Preferences.Default.ContainsKey("CurrentUserJson") ? Preferences.Default.Get("CurrentUserJson", string.Empty) : null;
                if (!string.IsNullOrEmpty(savedUserJson))
                {
                    try
                    {
                        var user = JsonSerializer.Deserialize<AppUser>(savedUserJson, sOptions);
                        if (user != null)
                        {
                            CurrentUser = user;
                            CurrentUserId = user.Id;
                            return;
                        }
                    }
                    catch { }
                }

                if (Application.Current?.MainPage is Page mainPage)
                {
                    await mainPage.Navigation.PushAsync(new Pages.Account.LoginPage());
                }
            }
            catch { }
        }

        internal async Task SynchronizeAll()
        {
            await LoginToAPI();

            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                await AllBooks();
            }
        }

        public class LoginModel
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public DateTime ValidTill { get; set; }
            public bool RememberMe { get; set; }
        }

        // DTO types for mobiledata endpoint
        private class MobileDataDto
        {
            public List<CategoryDto>? Categories { get; set; }
            public List<BookDto>? Books { get; set; }
            public List<MemberDto>? Members { get; set; }
            public List<LoanDto>? Loans { get; set; }
        }

        private class CategoryDto
        {
            public int Id { get; set; }
            public string? Naam { get; set; }
        }

        private class BookDto
        {
            public int Id { get; set; }
            public string? Titel { get; set; }
            public string? Auteur { get; set; }
            public string? Isbn { get; set; }
            public int CategorieId { get; set; }
            public string? CategorieNaam { get; set; }
        }

        private class MemberDto
        {
            public int Id { get; set; }
            public string? Voornaam { get; set; }
            public string? AchterNaam { get; set; }
            public string? Email { get; set; }
        }

        private class LoanDto
        {
            public int Id { get; set; }
            public int BoekId { get; set; }
            public string? BoekTitel { get; set; }
            public int LidId { get; set; }
            public string? LidNaam { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime? ReturnedAt { get; set; }
        }
    }
}
