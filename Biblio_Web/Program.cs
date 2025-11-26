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
using Biblio_Web.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string
const string ConnKey = "BibliobContextConnection";
var connectionString = builder.Configuration.GetConnectionString(ConnKey)
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? "Server=(localdb)\\mssqllocaldb;Database=BiblioDb;Trusted_Connection=True;MultipleActiveResultSets=true";

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources/Vertalingen");

// Configure DbContext and suppress pending-model-changes warning temporarily (create migrations instead)
builder.Services.AddDbContext<BiblioDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
);


// Identity + packaged UI
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

// Register Razor Pages and require authorization for Manage folder
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
})
.AddViewLocalization();

// JWT Authentication (register JwtBearer without overriding cookie defaults)
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

// Ensure the application cookie redirects unauthenticated browser requests to the Identity login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Admin", "Medewerker"));
    options.AddPolicy("RequireMember", policy => policy.RequireRole("Admin", "Medewerker", "Lid"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Biblio_Web.Mapping.MappingProfile));

// MVC with global authorization fallback
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
    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(Biblio_Web.SharedResource));
});

// Add controllers support
builder.Services.AddControllers();

// Swagger + JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Define a Swagger document
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Biblio API", Version = "v1" });

    // JWT Bearer authentication configuration for Swagger UI
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

    // Show example payload for the login endpoint in Swagger UI
    c.OperationFilter<AddLoginRequestExampleOperationFilter>();
});

// Supported cultures
var supportedCultures = new[] { new CultureInfo("nl"), new CultureInfo("en") };

var app = builder.Build();

// Request localization
var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl"),
    SupportedCultures = supportedCultures.ToList(),
    SupportedUICultures = supportedCultures.ToList()
};
locOptions.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider());

app.UseRequestLocalization(locOptions);

// Ensure database is created/migrated and seed roles/users in development
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<BiblioDbContext>();
        db.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        string[] roles = new[] { "Admin", "Medewerker", "Lid" };
        foreach (var r in roles)
        {
            var exists = roleManager.RoleExistsAsync(r).GetAwaiter().GetResult();
            if (!exists)
            {
                roleManager.CreateAsync(new IdentityRole(r)).GetAwaiter().GetResult();
            }
        }

        var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@biblio.local";
        var adminPwd = builder.Configuration["Seed:AdminPassword"] ?? "Admin123!";
        var admin = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (admin == null)
        {
            admin = new AppUser { UserName = adminEmail, Email = adminEmail, FullName = "Hoofdbeheerder" };
            var res = userManager.CreateAsync(admin, adminPwd).GetAwaiter().GetResult();
            if (res.Succeeded)
            {
                userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    // Serve the generated Swagger as JSON endpoint and enable Swagger UI
    app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblio API V1");
        c.RoutePrefix = "swagger"; // serve UI at /swagger
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers and Razor Pages so Identity UI is reachable
app.MapControllers();
app.MapRazorPages();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
