Biblio_Web\Middleware\CookiePolicyOptionsProvider.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Biblio_Web.Middleware
{
    public static class CookiePolicyOptionsProvider
    {
        public static CookiePolicyOptions CreateDefault()
        {
            return new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax,
                // Use fully-qualified enum to avoid ambiguous/unknown symbol errors
                HttpOnly = Microsoft.AspNetCore.Http.HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.SameAsRequest,
                // Zet op 'true' als je expliciete toestemming wilt afdwingen en een consent UI hebt
                CheckConsentNeeded = context => false
            };
        }
    }
}