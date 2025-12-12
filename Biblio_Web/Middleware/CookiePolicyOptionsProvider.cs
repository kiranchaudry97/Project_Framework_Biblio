using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace Biblio_Web.Middleware
{
    // Simple provider interface to return configured CookiePolicyOptions
    public interface ICookiePolicyOptionsProvider
    {
        CookiePolicyOptions GetOptions();
    }

    // Default implementation used when no custom provider is supplied.
    public class DefaultCookiePolicyOptionsProvider : ICookiePolicyOptionsProvider
    {
        public CookiePolicyOptions GetOptions()
        {
            return new CookiePolicyOptions
            {
                // require explicit consent for non-essential cookies unless a consent cookie is already present
                CheckConsentNeeded = ctx => !ctx.Request.Cookies.ContainsKey("biblio_cookie_consent"),
                MinimumSameSitePolicy = SameSiteMode.Lax,
                Secure = CookieSecurePolicy.SameAsRequest,
                OnAppendCookie = ctx => { /* no-op, hook for audits if needed */ },
                OnDeleteCookie = ctx => { /* no-op */ }
            };
        }
    }
}
