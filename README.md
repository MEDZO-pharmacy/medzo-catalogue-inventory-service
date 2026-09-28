# Medzo Catalogue and Inventory Service

.NET 10 Clean Architecture service containing Catalogue and Inventory as internal modules. It uses the `medzo_inventory_db` Azure SQL database, validates JWTs issued by Medzo Auth, consumes purchasing/sales events idempotently, and publishes inventory events through a transactional outbox.

## Run

Copy `.env.example` values into your local secret/configuration provider, then run:

```powershell
dotnet restore
dotnet tool run dotnet-ef -- database update --project src/Medzo.CatalogueInventory.Infrastructure --startup-project src/Medzo.CatalogueInventory.Api
dotnet run --project src/Medzo.CatalogueInventory.Api
```

Never commit the shared JWT signing secret or database password. The service has no connection to the Auth database.

### Development demonstration data

To populate the real development database with sample medicines, batches, low-stock data, purchase receipts, sale issues, and stock movements, set this in the local `.env` file:

```dotenv
SeedDemoData__Enabled=true
```

Start the API once. The seeder applies pending migrations and inserts its records only when they do not already exist. It runs only when `ASPNETCORE_ENVIRONMENT=Development`; keep it disabled in shared and production environments.

For local Catalogue/Inventory CRUD testing without Kafka, use `Kafka__Enabled=false`. Enable it only after a broker is running when testing purchase and sale events.

## Frontend

The React/Vite application is maintained in the separate `medzo-frontend` repository. Its Catalogue and Inventory features use this service through `VITE_CATALOGUE_INVENTORY_API_URL`.

```powershell
Set-Location <path-to-medzo-frontend>/frontend
Copy-Item .env.example .env
npm ci
npm run dev
```

Configure `VITE_AUTH_API_URL` and `VITE_CATALOGUE_INVENTORY_API_URL` in `frontend/.env`.
## CI/CD deployment

The workflow tests the `dev` branch, publishes an immutable GHCR image, and
deploys `medzo-catalogue-service` in `rg-MEDZO-NEW` using Azure OIDC. See the
[Azure GitHub OIDC setup](https://github.com/MEDZO-pharmacy/medzo-user-auth-service/blob/main/docs/AZURE_GITHUB_OIDC.md)
for the one-time Entra application and federated credential setup. Add these as
Actions **Variables** in this repository:

| Variable | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | Azure app registration application (client) ID |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | `f53c5182-c196-4e12-b31f-a6e5bfa6b5fd` |

The Container App must have these runtime settings, configured in Azure rather
than committed to Git:

```text
ConnectionStrings__CatalogueInventory=<secret reference to catalogue Azure SQL>
Jwt__Secret=<secret reference to the same signing key used by Auth>
Jwt__Issuer=MedzoAuthService
Jwt__Audience=MedzoClient
Cors__AllowedOrigins__0=https://brave-forest-045498500.3.azurestaticapps.net
Kafka__Enabled=false
Database__MigrateOnStartup=true
ASPNETCORE_ENVIRONMENT=Production
```

The app applies pending migrations before it starts accepting requests. Keep
the Container App at one maximum replica while startup migrations are enabled.
The workflow verifies `/health` after deployment.
