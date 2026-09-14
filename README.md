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

The GitHub Actions workflow at `.github/workflows/deploy-azure-app-service.yml`
builds and tests every pull request targeting `dev`. A push to `dev` builds the
container and deploys it only when the repository variable `DEPLOY_ENABLED` is
set to `true`.

Before enabling deployment, configure the following in this repository:

| Type | Name | Value |
| --- | --- | --- |
| Variable | `AZURE_REGISTRY_NAME` | Azure Container Registry resource name, without `.azurecr.io` |
| Variable | `AZURE_WEBAPP_NAME` | Azure App Service name for this service |
| Variable | `CATALOGUE_HEALTH_URL` | HTTPS service origin, without `/health` |
| Variable | `DEPLOY_ENABLED` | `true` after all configuration below is complete |
| Secret | `AZURE_CLIENT_ID` | Azure deployment identity client ID |
| Secret | `AZURE_TENANT_ID` | Azure tenant ID |
| Secret | `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| Secret | `CATALOGUE_DATABASE_CONNECTION` | Azure SQL connection string for `medzo_inventory_db` |

Create a GitHub `production` environment and configure Azure OIDC federation
for `repo:MEDZO-pharmacy/medzo-catalogue-inventory-service:environment:production`.
The deployment identity needs permission to push images to the registry and
deploy to the App Service. The App Service identity needs permission to pull
images from the registry.

Configure these App Service application settings separately. They are runtime
configuration and must not be committed to Git:

```text
ConnectionStrings__CatalogueInventory=<Azure SQL connection string>
Jwt__Secret=<same signing secret used by medzo-user-auth-service>
Jwt__Issuer=MedzoAuthService
Jwt__Audience=MedzoClient
Cors__AllowedOrigins__0=<frontend HTTPS origin>
Kafka__Enabled=false
ASPNETCORE_ENVIRONMENT=Production
```

The workflow applies pending Entity Framework migrations before deploying the
container. It uses `CATALOGUE_DATABASE_CONNECTION` only in the protected
deployment job. Do not enable the deployment until the Azure SQL server is
running and the GitHub-hosted runner can reach it through its firewall rules.
