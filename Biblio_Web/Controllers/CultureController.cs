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

            // Use explicit cookie options to ensure the cookie is set and available across the site.
            var options = new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                // Keep SameSite lax for insecure requests; require None for cross-site when secure.
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Secure = Request.IsHttps,
                HttpOnly = false
            };

            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, cookieValue, options);

            _logger.LogInformation("SetLanguage called. culture={culture}, returnUrl={returnUrl}, cookie={cookie}, isHttps={isHttps}", culture, returnUrl, cookieValue, Request.IsHttps);

            // Set TempData so layout can show a toast confirming the language change
            TempData["LanguageChanged"] = culture;

            // Normalize and sanitize returnUrl to avoid nested Culture/SetLanguage loops
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }
            else
            {
                try
                {
                    // decode repeatedly up to a few levels to detect nested SetLanguage chains
                    string decoded = returnUrl;
                    for (int i = 0; i < 5; i++)
                    {
                        var next = System.Net.WebUtility.UrlDecode(decoded);
                        if (string.Equals(next, decoded, StringComparison.Ordinal)) break;
                        decoded = next;
                    }

                    // If the decoded URL contains a culture set action or a ReturnUrl param, avoid redirect loops
                    if (!string.IsNullOrEmpty(decoded) && (decoded.IndexOf("/Culture/SetLanguage", StringComparison.OrdinalIgnoreCase) >= 0 || decoded.IndexOf("ReturnUrl=", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        _logger.LogWarning("Rejecting returnUrl '{ReturnUrl}' (decoded: '{Decoded}') because it contains nested SetLanguage or ReturnUrl.", returnUrl, decoded);
                        returnUrl = Url.Content("~/");
                    }
                    else
                    {
                        // Use the decoded value if it's safe
                        returnUrl = decoded;
                    }
                }
                catch (Exception ex)
                {
                    // log decode errors for debugging and fall back to safe root
                    _logger.LogWarning(ex, "Failed to decode returnUrl '{ReturnUrl}' - redirecting to root.", returnUrl);
                    returnUrl = Url.Content("~/");
                }
            }

            // Ensure local url only and starts with '/'
            if (!Url.IsLocalUrl(returnUrl) || !(returnUrl.StartsWith("/") || returnUrl.StartsWith("~/")))
            {
                _logger.LogWarning("Rejected returnUrl '{ReturnUrl}' because it is not a local path. Redirecting to root.", returnUrl);
                returnUrl = Url.Content("~/");
            }

            return LocalRedirect(returnUrl);
        }

        // New: clear the culture cookie so the site falls back to Accept-Language or default behavior
        [HttpGet]
        public IActionResult ResetLanguage(string? returnUrl)
        {
            try
            {
                // Remove the culture cookie by setting an expired cookie
                var options = new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(-1),
                    IsEssential = true,
                    SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    HttpOnly = false
                };

                Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, string.Empty, options);
                _logger.LogInformation("ResetLanguage called. Cleared culture cookie. returnUrl={returnUrl}", returnUrl);

                // Indicate reset so layout can optionally show a toast
                TempData["LanguageReset"] = "1";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ResetLanguage failed");
            }

            if (string.IsNullOrEmpty(returnUrl)) returnUrl = Url.Content("~/");
            if (!Url.IsLocalUrl(returnUrl) || !(returnUrl.StartsWith("/") || returnUrl.StartsWith("~/")))
            {
                returnUrl = Url.Content("~/");
            }

            return LocalRedirect(returnUrl);
        }
    }
}
