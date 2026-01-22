using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Biblio_Web.Middleware
{
    // Interface die cookie policy configuratie levert
    // Maakt het systeem uitbreidbaar en testbaar
    public interface ICookiePolicyOptionsProvider
    {
        CookiePolicyOptions GetOptions();
    }

    // Standaard implementatie van het cookie-beleid
    // Wordt gebruikt wanneer geen andere provider is geregistreerd
    public class DefaultCookiePolicyOptionsProvider : ICookiePolicyOptionsProvider
    {
        public CookiePolicyOptions GetOptions()
        {
            return new CookiePolicyOptions
            {
                // Vereist expliciete toestemming voor niet-essentiële cookies
                // tenzij de consent-cookie al bestaat
                CheckConsentNeeded = ctx =>
                    !ctx.Request.Cookies.ContainsKey("biblio_cookie_consent"),

                // Minimum SameSite-beleid (bescherming tegen CSRF)
                MinimumSameSitePolicy = SameSiteMode.Lax,

                // Cookies zijn secure als de request via HTTPS verloopt
                Secure = CookieSecurePolicy.SameAsRequest,

                // Hook voor logging of auditing bij het plaatsen van cookies
                OnAppendCookie = ctx => { },

                // Hook voor logging of auditing bij het verwijderen van cookies
                OnDeleteCookie = ctx => { }
            };
        }
    }
}

