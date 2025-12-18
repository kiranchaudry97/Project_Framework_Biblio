Secure database configuration (recommended)

Doel
----
Zorg dat de productie-/development database connection string niet in versiebeheer terechtkomt. Gebruik in plaats daarvan veilige opslag: user-secrets (development), environment variables of Azure App Service "Configuration" (production).

Aanbevolen opties
-----------------
1) Development — dotnet user-secrets (lokale, per-developer)
   - Ga naar de projectmap van het webproject (map met `Biblio_Web.csproj`).
   - Voer uit in PowerShell / terminal:
     ```powershell
     cd Biblio_Web
     # initialiseer user secrets (als nog niet gedaan)
     dotnet user-secrets init
     
     # typische sleutel: ConnectionStrings:DefaultConnection
     dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
     
     # of gebruik de legacy/public key die dit project ondersteunt (optioneel)
     dotnet user-secrets set "PublicConnection_Azure" "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
     ```
   - ASP.NET Core laadt user-secrets automatisch in Development wanneer `UserSecretsId` aanwezig is in the .csproj.

2) Local environment variable (temporary/session)
   - PowerShell (session):
     ```powershell
     $env:ConnectionStrings__DefaultConnection = 'Server=tcp:...;Initial Catalog=...;User ID=...;Password=...;'
     dotnet run --project Biblio_Web
     ```
   - Bash (session):
     ```bash
     export ConnectionStrings__DefaultConnection='Server=tcp:...;Initial Catalog=...;User ID=...;Password=...;'
     dotnet run --project Biblio_Web
     ```
   - Windows persistent (not recommended for secrets): `setx ConnectionStrings__DefaultConnection "..."`

3) Production / Hosted (Azure App Service)
   - In Azure Portal > App Service > Configuration > Application settings
     - Add a new setting with name `ConnectionStrings__DefaultConnection` (or `PublicConnection_Azure` if you prefer the legacy key) and value = your connection string. Save and restart the App Service.
   - Voor Azure App Service is het veiliger om Key Vault references of Managed Identity te gebruiken, zie sectie hieronder.

Azure Managed Identity / Key Vault (aanbevolen voor productie)
--------------------------------------------------------------
- Gebruik Managed Identity zodat de applicatie geen plaintext credentials hoeft te bezitten.
- Voor Azure SQL met Managed Identity kun je in sommige hosting-scenario's een connection string met `Authentication=Active Directory Default` gebruiken. Voorbeeld env var:
  ```powershell
  $env:AZURE_SQL_CONNECTIONSTRING = 'Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-db;Authentication=Active Directory Default;Encrypt=True;'
  ```
- Configureer een system-assigned managed identity voor je App Service en maak in de database een gebruiker aan:
  ```sql
  CREATE USER [<principal-name>] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [<principal-name>];
  ALTER ROLE db_datawriter ADD MEMBER [<principal-name>];
  ```
- Als je Key Vault gebruikt, bewaar de connection string (of credentials) in Key Vault en geef App Service toestemming om secrets te lezen via Key Vault references (of gebruik Managed Identity + Key Vault access policy).

Program.cs / DbContext configuratie — hoe het project jouw instellingen leest
---------------------------------------------------------------------------
- Het webproject (`Biblio_Web/Program.cs`) probeert connectionstrings in deze volgorde te vinden:
  1. `ConnectionStrings:BibliobContextConnection`
  2. `ConnectionStrings:DefaultConnection`
  3. `ConnectionStrings__DefaultConnection` (omgeving variabele)
  4. `AZURE_SQL_CONNECTIONSTRING` (voor managed identity / AD)
  5. `PublicConnection_Azure` of `PublicConnection` (legacy keys)
  6. fallback naar LocalDB (development)

- Concreet betekent dit dat je in development meestal `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` gebruikt en in productie environment variables/App Service settings of Key Vault.

Verifiëren
----------
- Start de webapp en controleer logs. `Program.cs` en `BiblioDbContext` schrijven debug‑meldingen welke bron gebruikt is.
- Controleer dat de seed-scripts draaien (SeedData.InitializeAsync) en dat admin-user is aangemaakt, of gebruik SQL tooling (Azure Data Studio) om te inspecteren.

Veiligheidstips
---------------
- Gebruik nooit repository-commits met credentials.
- Gebruik Azure Key Vault en Managed Identity voor productie.
- Revoke / roter wachtwoorden regelmatig.

Referenties
----------
- dotnet user-secrets: https://learn.microsoft.com/dotnet/core/extensions/user-secrets
- Azure App Service application settings: https://learn.microsoft.com/azure/app-service/configure-common
- Azure Key Vault & Managed Identity: https://learn.microsoft.com/azure/key-vault/general/overview
- Azure SQL firewall & connectivity: https://learn.microsoft.com/azure/azure-sql/
