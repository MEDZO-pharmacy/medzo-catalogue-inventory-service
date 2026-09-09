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

## Frontend

The React/Vite frontend is contained in `frontend/` and includes the shared Medzo pages plus the Catalogue and Inventory feature folders.

```powershell
Set-Location frontend
Copy-Item .env.example .env
npm ci
npm run dev
```

Configure `VITE_AUTH_API_URL` and `VITE_CATALOGUE_INVENTORY_API_URL` in `frontend/.env`.
