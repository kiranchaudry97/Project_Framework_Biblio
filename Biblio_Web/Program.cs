// Program.cs
// Doel: bootstrap en configuratie van de webapplicatie (services, middleware en routing).
// Gebruik: registreert services (DB, Identity, localization, AutoMapper, controllers, Swagger),
//         voert database migratie en seeding uit bij startup en bouwt RequestLocalizationOptions
//         op basis van talen in de database (met fallback).
// Doelstellingen:
// - Centraliseer startup-configuratie en maak cultuur/taal dynamisch instelbaar via de Taal-entiteit in de DB.
// - Zorg voor veilige defaults voor Identity en JWT, en configureer autorisatiepolicies.
// - Voer database migrations/seeding automatisch uit in development zodat de app direct getest kan worden.
// - Houd cookie-gedrag en lokaliseringsproviders consistent zodat gebruikerskeuzes persistent zijn.

using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.OpenApi.Models;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;
using Biblio_Models.Seed;
using Biblio_Web.Middleware; // added for cookie provider

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string
const string ConnKey = "BibliobContextConnection";
var connectionString = builder.Configuration.GetConnectionString(ConnKey)
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

// Localisatie - gebruik de map Vertalingen (bevat de volledige SharedResource.*.resx bestanden)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources/Vertalingen");

// Register default cookie policy options provider
builder.Services.AddSingleton<ICookiePolicyOptionsProvider, DefaultCookiePolicyOptionsProvider>();

// DbContext configureren en tijdelijk waarschuwing over pending model changes onderdrukken (maak migrations aan)
builder.Services.AddDbContext<BiblioDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
);


// Identity + standaard UI
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<BiblioDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Register Razor Pages en eis autorisatie voor de Manage-folder van Identity
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
})
.AddViewLocalization()
.AddDataAnnotationsLocalization(options =>
{
    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(Biblio_Web.SharedResource));
});

// JWT Authenticatie (registreer JwtBearer zonder cookie-standaarden te overschrijven)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "SuperSecretDevelopmentKey";
var jwtIssuer = jwtSection["Issuer"] ?? "BiblioApp";

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Zorg dat de application cookie niet-geauthenticeerde browserverzoeken doorstuurt naar de Identity loginpagina
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
});

// Autorisatiepolicies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Admin", "Medewerker"));
    options.AddPolicy("RequireMember", policy => policy.RequireRole("Admin", "Medewerker", "Lid"));
});

// AutoMapper configuratie
builder.Services.AddAutoMapper(typeof(Biblio_Web.Mapping.MappingProfile));

// MVC met globale autorisatiefallback
builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
})
.AddViewLocalization()
.AddDataAnnotationsLocalization(options =>
{
    // gebruik de SharedResource class voor gevalideerde/localized data annotations
    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(Biblio_Web.SharedResource));
});

// Controllers ondersteuning
builder.Services.AddControllers();

// Swagger + JWT ondersteuning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Definieer een Swagger document
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Biblio API", Version = "v1" });

    // JWT Bearer authentication configuratie voor Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "bearer",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

var supportedCultures = new[] { "nl", "en", "fr" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
localizationOptions.RequestCultureProviders.Insert(1, new QueryStringRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

// Configureer de HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    // Serveer het gegenereerde Swagger als JSON endpoint en enable Swagger UI
    app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblio API V1");
        c.RoutePrefix = "swagger"; // toon UI op /swagger
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Apply cookie policy from provider (ensures consent handling and same-site/secure defaults)
try
{
    var provider = app.Services.GetService<ICookiePolicyOptionsProvider>();
    var opts = provider?.GetOptions() ?? new CookiePolicyOptions();
    app.UseCookiePolicy(opts);
}
catch { /* ignore if provider not available */ }

app.UseAuthentication();
app.UseAuthorization();

// Map controllers en Razor Pages zodat de Identity UI bereikbaar is
app.MapControllers();
app.MapRazorPages();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
