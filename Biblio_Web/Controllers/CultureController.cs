using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Biblio_Web.Controllers
{
    public class CultureController : Controller
    {
        private readonly ILogger<CultureController> _logger;

        public CultureController(ILogger<CultureController> logger)
        {
            _logger = logger;
        }

        // Supports GET requests from the language links. Sets a culture cookie and redirects back.
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            if (string.IsNullOrEmpty(culture))
                culture = "nl";

            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
            );

            _logger.LogInformation("SetLanguage called. culture={culture}, returnUrl={returnUrl}, cookie={cookie}", culture, returnUrl, cookieValue);

            // Set TempData so layout can show a toast confirming the language change
            TempData["LanguageChanged"] = culture;

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Content("~/");

            return LocalRedirect(returnUrl);
        }
    }
}
