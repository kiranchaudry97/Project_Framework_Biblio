Azure Managed Identity + Azure SQL — quick CLI guide

Doel
----
Configureer een App Service (of andere compute) om via een Managed Identity (system-assigned) veilig verbinding te maken met Azure SQL zonder plaintext wachtwoorden.

Belangrijkste stappen (kort)
1. Schakel system-assigned Managed Identity in op je App Service
2. Maak een database gebruiker voor die identity en geef rechten (db_datareader/db_datawriter)
3. Stel connection string in als App Setting met `Authentication=Active Directory Default` (zonder user/password)
4. Deploy en verifieer logs

Voorwaarden
- Azure CLI geïnstalleerd en geauthenticeerd (az login)
- Je bent eigenaar of hebt rights om identiteiten en SQL-rollen toe te kennen
- Je hebt servernaam (biblio-sql-server), database-naam (Biblio-sql-db-6055956), resource-group en app service name

CLI stappen (vervang de placeholders)

# 1) Schakel system-assigned managed identity in voor je App Service
az webapp identity assign \
  --resource-group <RESOURCE_GROUP> \
  --name <APP_NAME>

# 2) Noteer principalId van de identity (service principal object id)
az webapp identity show \
  --resource-group <RESOURCE_GROUP> \
  --name <APP_NAME> \
  --query principalId -o tsv

# 3) Zorg dat Azure AD admin op de SQL server is ingesteld (een Azure AD user of group). Als dat nog niet is gedaan:
# (zorg dat je een Azure AD admin kunt instellen via portal of CLI)
az sql server ad-admin create \
  --resource-group <RESOURCE_GROUP> \
  --server-name <SQL_SERVER_NAME> \
  --display-name "<AAD_ADMIN_USER>" \
  --object-id <AAD_ADMIN_OBJECT_ID>

# 4) Maak in de database een database-gebruiker voor de managed identity (voer dit uit met een account dat admin rechten op de DB heeft,
#    bijvoorbeeld via de Query Editor (portal) of via sqlcmd met server admin credentials)

-- SQL statements (voer uit in de context van database Biblio-sql-db-6055956):
CREATE USER [<APP_PRINCIPAL_NAME>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<APP_PRINCIPAL_NAME>];
ALTER ROLE db_datawriter ADD MEMBER [<APP_PRINCIPAL_NAME>];

# 5) Stel de connection string in App Service Configuration (no secrets)
# Gebruik Authentication=Active Directory Default
az webapp config appsettings set \
  --resource-group <RESOURCE_GROUP> \
  --name <APP_NAME> \
  --settings "AZURE_SQL_CONNECTIONSTRING=Server=tcp:<SQL_SERVER_NAME>.database.windows.net,1433;Initial Catalog=<DATABASE_NAME>;Authentication=Active Directory Default;Encrypt=True;"

# 6) Herstart de App Service
az webapp restart --resource-group <RESOURCE_GROUP> --name <APP_NAME>

Verifiëren
- Tail logs:
  az webapp log tail --resource-group <RESOURCE_GROUP> --name <APP_NAME>
- Controleer dat connection wordt gemaakt en dat geen password-fouten optreden

Notities en valkuilen
- Je App Service identity moet een database-gebruiker hebben in *die* database (CREATE USER ... FROM EXTERNAL PROVIDER).
- Als je wilt dat de app ook migrations uitvoert bij startup, geef de identity de benodigde rechten (of voer migrations handmatig uit met een admin account).
- Lokale ontwikkeling: DefaultAzureCredential gebruikt VS/az CLI credentials. Voor lokale testing is het vaak handiger user-secrets te gebruiken met SQL-auth.

Als je wilt dat ik de exacte Azure CLI-commando’s invul met jouw `resource-group` en `app-name` en (optioneel) `aad-admin`/`principal-name`, geef die waarden dan. Ik kan dan de commands kant-en-klaar genereren.
