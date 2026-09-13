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
