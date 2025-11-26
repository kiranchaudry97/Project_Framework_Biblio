using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace Biblio_Web.Controllers
{
    public class CultureController : Controller
    {
        // Supports GET requests from the language links. Sets a culture cookie and redirects back.
        [HttpGet]
        [AllowAnonymous]
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

            // If returnUrl not provided, try to use Referer header so links without explicit returnUrl still return to the originating page
            if (string.IsNullOrEmpty(returnUrl))
            {
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    // Use the path + query portion if the referer is an absolute URL
                    if (Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                        returnUrl = uri.PathAndQuery;
                    else
                        returnUrl = referer;
                }
            }

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Content("~/");

            // Ensure we only redirect to local URLs
            if (!Url.IsLocalUrl(returnUrl))
                return LocalRedirect(Url.Content("~/"));

            return LocalRedirect(returnUrl);
        }
    }
}
