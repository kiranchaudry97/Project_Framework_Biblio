Voorbeeld: SeedOptions via User Secrets (Nederlands)

Doel
-----
Voor ontwikkelomgevingen mogen testaccounts en dev-wachtwoorden niet in de repository staan. Gebruik User Secrets of appsettings (niet in Git) om de waarden voor `SeedOptions` te zetten.

Voorbeeldconfiguratie (JSON) — gebruik dit als inhoud voor `dotnet user-secrets` of als lokale `appsettings.Development.json` (NIET commiten):

```json
{
  "Seed": {
    "CreateTestAccounts": true,
    "AdminEmail": "admin@biblio.local",
    "AdminPassword": "Admin1234?",
    "AdminFullName": "Beheerder",

    "StaffEmail": "medewerker@biblio.local",
    "StaffPassword": "test1234?",

    "BlockedEmail": "blocked@biblio.local",
    "BlockedPassword": "Test!23456"
  }
}
```

Aanbeveling (Windows / dotnet CLI)
----------------------------------
1) Zet user-secrets aan voor het WPF-project (voer uit in `Biblio_WPF` folder):

```powershell
# Doe dit éénmalig per project
dotnet user-secrets init
```

2) Stel de waarden in (voorbeeld):

```powershell
dotnet user-secrets set "Seed:CreateTestAccounts" "true"
dotnet user-secrets set "Seed:AdminEmail" "admin@biblio.local"
dotnet user-secrets set "Seed:AdminPassword" "Admin1234?"
# en zo verder voor staff/blocked
```

Veiligheid
---------
- Zet `CreateTestAccounts` op `false` in productie.
- Gebruik sterke wachtwoorden of sla deze enkel op in uw CI/CD secrets vault.
- Verwijder test-accounts uit de seed of zet seeding uit voor productie.

Gebruik
-----
SeedData leest `SeedOptions` via DI; wanneer `CreateTestAccounts` true is, worden staff/blocked accounts aangemaakt met de opgegeven credentials.

Kort: bewaar nooit echte wachtwoorden in de repo. Gebruik user secrets of een geheimenbeheer (Azure Key Vault, GitHub Secrets) voor productie-gevoelige waarden.