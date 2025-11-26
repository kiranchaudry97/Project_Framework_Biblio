using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Biblio_Web.Controllers
{
    public class CultureController : Controller
    {
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

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Content("~/");

            return LocalRedirect(returnUrl);
        }
    }
}
