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
     # voer dit in de root van het repo of ergens anders, pad naar project nodig
     cd Biblio_Web
     dotnet user-secrets set "PublicConnection_Azure" "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
     ```
   - ASP.NET Core laadt user-secrets automatisch in Development wanneer `UserSecretsId` aanwezig is in the .csproj.

2) Local environment variable (temporary/session)
   - PowerShell (session):
     ```powershell
     $env:PublicConnection_Azure = 'Server=tcp:...;Initial Catalog=...;User ID=...;Password=...;'
     dotnet run --project Biblio_Web
     ```
   - Windows persistent (not recommended for secrets): `setx PublicConnection_Azure "..."`

3) Production / Hosted (Azure App Service)
   - In Azure Portal > App Service > Configuration > Application settings
     - Add a new setting with name `PublicConnection_Azure` and value = your connection string. Save and restart the App Service.
   - For Azure App Service, prefer using Key Vault references or Managed Identity to avoid storing plaintext credentials.

Firewall / networking
---------------------
- Zorg dat de Azure SQL server firewall toestaat dat je development IP of je App Service toegang heeft.
- Voor App Service kun je:
  - "Allow Azure services and resources to access this server" inschakelen (let op security), of
  - gebruik een Virtual Network / Private Endpoint en configureer App Service integratie.

Verifiëren
----------
- Start de webapp en controleer logs. `Program.cs` logt welke connection-config is gebruikt (console). 
- Controleer dat de seed-scripts draaien (SeedData.InitializeAsync) en dat admin-user is aangemaakt, of gebruik SQL tooling (Azure Data Studio) om te inspecteren.

Veiligheidstips
---------------
- Gebruik nooit repository-commits met credentials.
- Gebruik Azure Key Vault en Managed Identity voor productie.
- Verwijder test-wachtwoorden uit user-secrets / prod-config als ze niet meer nodig zijn.

Referenties
----------
- dotnet user-secrets: https://learn.microsoft.com/dotnet/core/extensions/user-secrets
- Azure App Service application settings: https://learn.microsoft.com/azure/app-service/configure-common
- Azure SQL firewall & connectivity: https://learn.microsoft.com/azure/azure-sql/
