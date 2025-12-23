// Program.cs
// Doel: bootstrap en configuratie van de webapplicatie (services, middleware en routing).
// Program.cs
// The method bodies, field initializers, and property accessor bodies have been eliminated for brevity.
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
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string ConnKey = "BibliobContextConnection";

// Prefer explicit Azure connection settings when provided. Support two patterns:
// - AZURE_SQL_CONNECTIONSTRING: connection string key used for AAD / managed identity scenarios
// - PublicConnection_Azure / PublicConnection: legacy key used for SQL auth
var azureNamedConn = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING") ?? builder.Configuration["AZURE_SQL_CONNECTIONSTRING"];
var publicConnectionAzure = builder.Configuration["PublicConnection_Azure"];
var publicConnection = builder.Configuration["PublicConnection"];

var connectionString = builder.Configuration.GetConnectionString(ConnKey)
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    // prefer explicit AZURE_SQL_CONNECTIONSTRING when available (can contain Authentication=Active Directory Default)
    ?? (!string.IsNullOrWhiteSpace(azureNamedConn) ? azureNamedConn :
        // prefer Azure-specific public connection when provided
        (!string.IsNullOrWhiteSpace(publicConnectionAzure) ? publicConnectionAzure :
            (!string.IsNullOrWhiteSpace(publicConnection) ? publicConnection : null)))
    ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

// Log resolved connection source for diagnostics
try
{
    var logger = LoggerFactory.Create(l => l.AddConsole()).CreateLogger("Program.Connection");
    if (!string.IsNullOrWhiteSpace(azureNamedConn)) logger.LogInformation("Using AZURE_SQL_CONNECTIONSTRING for DB: {cs}", azureNamedConn);
    else if (!string.IsNullOrWhiteSpace(publicConnectionAzure)) logger.LogInformation("Using PublicConnection_Azure for DB: {cs}", publicConnectionAzure);
    else if (!string.IsNullOrWhiteSpace(publicConnection)) logger.LogInformation("Using PublicConnection for DB: {cs}", publicConnection);
    else logger.LogInformation("Using ConnectionString from config or localdb fallback.");
}
catch { }

// NOTE: If you intend to use Azure AD / Managed Identity authentication, set AZURE_SQL_CONNECTIONSTRING to a value
// like: "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-db;Authentication=Active Directory Default;Encrypt=True;"
// Then configure your App Service with a system-assigned managed identity and create a database user from the external provider:
//   CREATE USER [<principal-name>] FROM EXTERNAL PROVIDER;
//   ALTER ROLE db_datareader ADD MEMBER [<principal-name>];
//   ALTER ROLE db_datawriter ADD MEMBER [<principal-name>];
// See Biblio_Web/Azure_Managed_Identity.md for CLI steps.

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
    // Require confirmed email before allowing sign-in
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddEntityFrameworkStores<BiblioDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender, Biblio_Web.Services.DevEmailSender>();
}

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
})
.AddViewLocalization()
.AddDataAnnotationsLocalization(options =>
{
    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(Biblio_Web.SharedResource));
})
// Ensure JSON options applied for Razor Pages as well
.AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opts.JsonSerializerOptions.MaxDepth = 64;
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
})
.AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opts.JsonSerializerOptions.MaxDepth = 64;
});

// Controllers ondersteuning
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.MaxDepth = 64;
    });

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

try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Run the shared seed initializer (creates roles, admin user and data)
            SeedData.InitializeAsync(services).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Program");
            logger?.LogError(ex, "Database seeding failed at startup.");
            // Swallow to allow the app to start but errors are logged
        }
    }
}
catch (Exception)
{
    // don't prevent app from starting if seeding fails; errors already logged
}

app.Run();
