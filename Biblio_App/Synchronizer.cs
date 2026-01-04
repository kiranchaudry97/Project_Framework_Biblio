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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDbContextFactory<LocalDbContext> _dbContextFactory;
        private readonly string? _apiBase;
        private readonly JsonSerializerOptions _jsonOptions;

        internal AppUser? CurrentUser { get; private set; }
        internal string? CurrentUserId { get; private set; }

        public Synchronizer(IHttpClientFactory httpClientFactory, IDbContextFactory<LocalDbContext> dbContextFactory, string? apiBase = null)
        {
            _httpClientFactory = httpClientFactory;
            _dbContextFactory = dbContextFactory;
            _apiBase = apiBase;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
        }

        // Synchronize books from remote to local (best-effort). Does not remove local-only changes.
        async Task AllBooks()
        {
            using var db = _dbContextFactory.CreateDbContext();
            var client = _httpClientFactory.CreateClient("ApiWithToken");

            if (await IsAuthorized(client) && client.BaseAddress != null)
            {
                try
                {
                    var response = await client.GetAsync("api/mobiledata");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<MobileDataDto>(responseBody, _jsonOptions);
                    if (data != null)
                    {
                        // Upsert categories
                        if (data.Categories != null)
                        {
                            foreach (var c in data.Categories)
                            {
                                var existingC = await db.Categorien.FirstOrDefaultAsync(x => x.Id == c.Id);
                                if (existingC != null)
                                {
                                    existingC.Naam = c.Naam ?? existingC.Naam;
                                    existingC.IsDeleted = false;
                                }
                                else
                                {
                                    db.Categorien.Add(new Categorie { Id = c.Id, Naam = c.Naam ?? string.Empty });
                                }
                            }
                        }

                        // --- Synchronisatie: lokale boeken exact gelijk aan server ---
                        if (data.Books != null)
                        {
                            var serverBookIds = data.Books.Select(b => b.Id).ToHashSet();
                            var localBooks = await db.Boeken.Where(b => !b.IsDeleted).ToListAsync();

                            // Verwijder lokale boeken die niet meer op de server staan
                            foreach (var local in localBooks)
                            {
                                if (!serverBookIds.Contains(local.Id))
                                {
                                    db.Boeken.Remove(local);
                                }
                            }

                            // Upsert serverboeken
                            foreach (var b in data.Books)
                            {
                                var existing = await db.Boeken.FirstOrDefaultAsync(x => x.Id == b.Id);
                                if (existing != null)
                                {
                                    existing.Titel = b.Titel ?? string.Empty;
                                    existing.Auteur = b.Auteur ?? string.Empty;
                                    existing.Isbn = b.Isbn ?? string.Empty;
                                    existing.CategorieID = b.CategorieId;
                                    existing.CategorieNaam = b.CategorieNaam ?? string.Empty;
                                    existing.IsDeleted = false;
                                }
                                else
                                {
                                    db.Boeken.Add(new Boek
                                    {
                                        Id = b.Id,
                                        Titel = b.Titel ?? string.Empty,
                                        Auteur = b.Auteur ?? string.Empty,
                                        Isbn = b.Isbn ?? string.Empty,
                                        CategorieID = b.CategorieId,
                                        CategorieNaam = b.CategorieNaam ?? string.Empty,
                                        IsDeleted = false
                                    });
                                }
                            }
                        }

                        // Upsert members
                        if (data.Members != null)
                        {
                            foreach (var m in data.Members)
                            {
                                var existingM = await db.Leden.FirstOrDefaultAsync(x => x.Id == m.Id);
                                if (existingM != null)
                                {
                                    existingM.Voornaam = m.Voornaam ?? existingM.Voornaam;
                                    existingM.AchterNaam = m.AchterNaam ?? existingM.AchterNaam;
                                    existingM.Email = m.Email ?? existingM.Email;
                                    existingM.IsDeleted = false;
                                }
                                else
                                {
                                    db.Leden.Add(new Lid
                                    {
                                        Id = m.Id,
                                        Voornaam = m.Voornaam ?? string.Empty,
                                        AchterNaam = m.AchterNaam ?? string.Empty,
                                        Email = m.Email
                                    });
                                }
                            }
                        }

                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // best-effort: ignore failures
                }
            }
        }

        internal async Task<bool> IsAuthorized(HttpClient? client = null)
        {
            if (!string.IsNullOrEmpty(CurrentUserId))
                return true;

            client ??= _httpClientFactory.CreateClient("ApiWithToken");
            if (client.BaseAddress == null)
                return false;

            try
            {
                var resp = await client.GetAsync("api/auth/isauthorized");
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<AppUser>(body, _jsonOptions);
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
            using var db = _dbContextFactory.CreateDbContext();
            try
            {
                await db.Database.MigrateAsync();

                if (!await db.Categorien.AnyAsync())
                {
                    db.Categorien.AddRange(
                        new Categorie { Naam = "Roman" },
                        new Categorie { Naam = "Jeugd" },
                        new Categorie { Naam = "Thriller" },
                        new Categorie { Naam = "Wetenschap" }
                    );
                    await db.SaveChangesAsync();
                }

                if (!await db.Boeken.AnyAsync())
                {
                    var roman = await db.Categorien.FirstAsync(c => c.Naam == "Roman");
                    var jeugd = await db.Categorien.FirstAsync(c => c.Naam == "Jeugd");

                    db.Boeken.AddRange(
                        new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                        new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                        new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id },
                        new Boek { Titel = "Het Achterhuis", Auteur = "Anne Frank", Isbn = "9789047518314", CategorieID = roman.Id },
                        new Boek { Titel = "De Kleine Prins", Auteur = "Antoine de Saint-Exupéry", Isbn = "9789021677146", CategorieID = jeugd.Id },
                        new Boek { Titel = "Harry Potter en de Steen der Wijzen", Auteur = "J.K. Rowling", Isbn = "9789076174082", CategorieID = jeugd.Id },
                        new Boek { Titel = "De Da Vinci Code", Auteur = "Dan Brown", Isbn = "9789024546883", CategorieID = roman.Id }
                    );
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        internal async Task<bool> Login(LoginModel loginModel)
        {
            var client = _httpClientFactory.CreateClient("ApiWithToken");
            if (client.BaseAddress == null)
                return false; // no API configured

            try
            {
                var jsonString = JsonSerializer.Serialize(loginModel, _jsonOptions);
                var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/auth/login", content);
                if (!response.IsSuccessStatusCode)
                    return false;

                var respBody = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<AppUser>(respBody, _jsonOptions);
                if (user != null)
                {
                    CurrentUser = user;
                    CurrentUserId = user.Id;

                    // persist minimal info in preferences
                    try { Preferences.Default.Set("CurrentUserId", CurrentUserId ?? string.Empty); } catch { }
                    try { Preferences.Default.Set("CurrentUserJson", JsonSerializer.Serialize(user, _jsonOptions)); } catch { }

                    return true;
                }
            }
            catch { }

            return false;
        }

        private async Task LoginToAPI()
        {
            var client = _httpClientFactory.CreateClient("ApiWithToken");
            if (await IsAuthorized(client))
                return;

            try
            {
                var savedUserJson = Preferences.Default.ContainsKey("CurrentUserJson") ? Preferences.Default.Get("CurrentUserJson", string.Empty) : null;
                if (!string.IsNullOrEmpty(savedUserJson))
                {
                    try
                    {
                        var user = JsonSerializer.Deserialize<AppUser>(savedUserJson, _jsonOptions);
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
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await mainPage.Navigation.PushAsync(new Pages.Account.LoginPage());
                    });
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
