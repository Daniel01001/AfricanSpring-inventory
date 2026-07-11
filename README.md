# AfricanSpring Ice — Inventory & Tracking

Internal, mobile-first web app for tracking an ice business: stores/pipeline,
deliveries, payments (running balance), and stock (ledger). ASP.NET Core Razor
Pages on .NET 10, EF Core + PostgreSQL, deployable to Render's free tier.

## Run locally

You need a PostgreSQL database. Pick one:

### Option A — Docker (local)
```bash
docker compose up -d          # starts Postgres on localhost:5432
dotnet run                    # app migrates + seeds on startup
```

### Option B — Free cloud Postgres (Neon / Supabase)
Create a free database, then point the app at it and run:
```bash
# PowerShell
$env:DATABASE_URL = "postgres://user:pass@host/dbname"
dotnet run
```
The app parses `DATABASE_URL` (Render/Neon/Supabase style) automatically.

App runs at the URL shown in the console (e.g. http://localhost:5xxx).

## Querying the database
Connection (local Docker): `Host=localhost;Port=5432;Database=africanspring;Username=postgres;Password=postgres`

```bash
# psql via the container
docker exec -it africanspring-db psql -U postgres -d africanspring
```
Or connect any client (DBeaver, pgAdmin, Rider) with the details above.
Cloud DBs (Neon/Supabase) include a browser SQL editor.

## First login
Seeded on an empty database:
- **Daniel** — Owner (read-only dashboard) — PIN `1234`
- **Friend** — Friend (logs deliveries/payments/stock) — PIN `0000`

Override seed PINs with env vars `SEED_OWNER_PIN` / `SEED_FRIEND_PIN`. Change them after first login.

## Deploy to Render
1. Push this repo to GitHub.
2. Render → New → Web Service → connect the repo (auto-detects .NET).
3. Add a free Render PostgreSQL; Render injects `DATABASE_URL`.
4. Deploy. Migrations + seed run automatically on first boot.

## Data model
`Users, Stores, Products, Deliveries, DeliveryItems, Payments, Fridges, StockMovements`.
Outstanding per store = Σ delivered − Σ paid. Stock on hand = Σ signed `StockMovements`.
Saving a delivery auto-creates negative stock movements linked to it.
