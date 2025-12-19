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

        readonly LocalDbContext _context;
        readonly string? _apiBase;

        // Optional current user state kept locally in the synchronizer
        internal AppUser? CurrentUser { get; private set; }
        internal string? CurrentUserId { get; private set; }

        internal Synchronizer(LocalDbContext context, string? apiBase = null)
        {
            _context = context;
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

        // Synchronize books from remote to local (best-effort). Does not remove local-only changes.
        async Task AllBooks()
        {
            // Synchronize local changes to API: not implemented here

            // Synchronize from API to local if authorized and API configured
            if (await IsAuthorized() && client.BaseAddress != null)
            {
                try
                {
                    var response = await client.GetAsync("api/books");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var books = JsonSerializer.Deserialize<List<Boek>>(responseBody, sOptions);
                    if (books != null && books.Count > 0)
                    {
                        foreach (var book in books)
                        {
                            var existing = await _context.Boeken.FirstOrDefaultAsync(b => b.Id == book.Id);
                            if (existing != null)
                            {
                                _context.Entry(existing).CurrentValues.SetValues(book);
                            }
                            else
                            {
                                _context.Boeken.Add(book);
                            }
                        }
                        await _context.SaveChangesAsync();
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
                await _context.Database.MigrateAsync();

                if (!await _context.Categorien.AnyAsync())
                {
                    _context.Categorien.AddRange(
                        new Categorie { Naam = "Roman" },
                        new Categorie { Naam = "Jeugd" },
                        new Categorie { Naam = "Thriller" },
                        new Categorie { Naam = "Wetenschap" }
                    );
                    await _context.SaveChangesAsync();
                }

                if (!await _context.Boeken.AnyAsync())
                {
                    var roman = await _context.Categorien.FirstAsync(c => c.Naam == "Roman");
                    var jeugd = await _context.Categorien.FirstAsync(c => c.Naam == "Jeugd");

                    _context.Boeken.AddRange(
                        new Boek { Titel = "1984", Auteur = "George Orwell", Isbn = "9780451524935", CategorieID = roman.Id },
                        new Boek { Titel = "De Hobbit", Auteur = "J.R.R. Tolkien", Isbn = "9780547928227", CategorieID = roman.Id },
                        new Boek { Titel = "Matilda", Auteur = "Roald Dahl", Isbn = "9780142410370", CategorieID = jeugd.Id }
                    );
                    await _context.SaveChangesAsync();
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
    }
}
