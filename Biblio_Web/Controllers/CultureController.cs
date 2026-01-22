using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Biblio_Web.Controllers
{
    // Controller verantwoordelijk voor taal- en cultuurinstellingen
    // Werkt via cookies en ASP.NET Core localization middleware
    public class CultureController : Controller
    {
        // Logger voor debugging en auditing
        private readonly ILogger<CultureController> _logger;

        // Dependency Injection van ILogger
        public CultureController(ILogger<CultureController> logger)
        {
            _logger = logger;
        }

        // =====================================================
        // SET LANGUAGE
        // =====================================================
        // Wordt aangeroepen via taal-links (bv. NL / EN / FR)
        // Zet een culture-cookie en stuurt de gebruiker terug
        // GET: /Culture/SetLanguage?culture=fr&returnUrl=/Boeken
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            // Fallback naar Nederlands indien geen cultuur werd meegegeven
            if (string.IsNullOrEmpty(culture))
                culture = "nl";

            // Maak cookie-waarde aan volgens ASP.NET Core standaard
            var cookieValue =
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture));

            // Cookie-opties
            var options = new CookieOptions
            {
                // Cookie beschikbaar over de hele site
                Path = "/",

                // Geldig voor 1 jaar
                Expires = DateTimeOffset.UtcNow.AddYears(1),

                // Essentieel (GDPR: geen consent vereist)
                IsEssential = true,

                // SameSite-instelling:
                // - HTTPS → None (cross-site mogelijk)
                // - HTTP  → Lax
                SameSite = Request.IsHttps
                    ? SameSiteMode.None
                    : SameSiteMode.Lax,

                // Secure cookie enkel bij HTTPS
                Secure = Request.IsHttps,

                // Cookie is leesbaar voor JS (bv. UI feedback)
                HttpOnly = false
            };

            // Voeg de cultuur-cookie toe aan de response
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                options);

            // Log taalwissel
            _logger.LogInformation(
                "SetLanguage called. culture={culture}, returnUrl={returnUrl}, cookie={cookie}, isHttps={isHttps}",
                culture,
                returnUrl,
                cookieValue,
                Request.IsHttps);

            // TempData voor UI-feedback (bv. toastmelding)
            TempData["LanguageChanged"] = culture;

            // -------------------------------------------------
            // ReturnUrl normaliseren en beveiligen
            // -------------------------------------------------

            // Indien geen returnUrl → naar home
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }
            else
            {
                try
                {
                    // Decode meerdere keren om geneste ReturnUrl's te detecteren
                    string decoded = returnUrl;
                    for (int i = 0; i < 5; i++)
                    {
                        var next = System.Net.WebUtility.UrlDecode(decoded);
                        if (string.Equals(next, decoded, StringComparison.Ordinal))
                            break;
                        decoded = next;
                    }

                    // Vermijd redirect-loops naar SetLanguage
                    if (!string.IsNullOrEmpty(decoded) &&
                        (decoded.IndexOf("/Culture/SetLanguage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         decoded.IndexOf("ReturnUrl=", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        _logger.LogWarning(
                            "Rejecting returnUrl '{ReturnUrl}' (decoded: '{Decoded}') because it contains nested SetLanguage or ReturnUrl.",
                            returnUrl,
                            decoded);

                        returnUrl = Url.Content("~/");
                    }
                    else
                    {
                        returnUrl = decoded;
                    }
                }
                catch (Exception ex)
                {
                    // Fallback bij decode-fouten
                    _logger.LogWarning(
                        ex,
                        "Failed to decode returnUrl '{ReturnUrl}' - redirecting to root.",
                        returnUrl);

                    returnUrl = Url.Content("~/");
                }
            }

            // Extra beveiliging: alleen lokale URLs toestaan
            if (!Url.IsLocalUrl(returnUrl) ||
                !(returnUrl.StartsWith("/") || returnUrl.StartsWith("~/")))
            {
                _logger.LogWarning(
                    "Rejected returnUrl '{ReturnUrl}' because it is not a local path. Redirecting to root.",
                    returnUrl);

                returnUrl = Url.Content("~/");
            }

            // Veilige redirect
            return LocalRedirect(returnUrl);
        }

        // =====================================================
        // RESET LANGUAGE
        // =====================================================
        // Verwijdert de cultuur-cookie zodat de app terugvalt
        // op Accept-Language of de standaardcultuur
        // GET: /Culture/ResetLanguage
        [HttpGet]
        public IActionResult ResetLanguage(string? returnUrl)
        {
            try
            {
                // Cookie verwijderen door een verlopen cookie te zetten
                var options = new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(-1),
                    IsEssential = true,
                    SameSite = Request.IsHttps
                        ? SameSiteMode.None
                        : SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    HttpOnly = false
                };

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    string.Empty,
                    options);

                _logger.LogInformation(
                    "ResetLanguage called. Cleared culture cookie. returnUrl={returnUrl}",
                    returnUrl);

                // UI-feedback
                TempData["LanguageReset"] = "1";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ResetLanguage failed");
            }

            // Fallback redirect
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Content("~/");

            // Enkel lokale URLs toestaan
            if (!Url.IsLocalUrl(returnUrl) ||
                !(returnUrl.StartsWith("/") || returnUrl.StartsWith("~/")))
            {
                returnUrl = Url.Content("~/");
            }

            return LocalRedirect(returnUrl);
        }
    }
}
