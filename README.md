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
- **Daniel** — Owner — PIN `1234`
- **Mongezi**, **Minenhle** — Friends — PIN `0000`

Override seed PINs with env vars `SEED_OWNER_PIN` / `SEED_FRIEND_PIN`. The Owner can
manage people (add / disable / reset PIN) in-app under **People**.

## Deploy to Render (blueprint)

The repo ships a `Dockerfile` and `render.yaml` that provision the web service **and**
a free Postgres, wired together.

1. Push this repo to GitHub.
2. Render → **New → Blueprint** → pick the repo. Render reads `render.yaml`, creates
   `africanspring-ice` (Docker web service) + `africanspring-db` (free Postgres), and
   injects `DATABASE_URL` automatically.
3. (Optional) Set `SEED_OWNER_PIN` / `SEED_FRIEND_PIN` in the service's Environment tab
   before the first deploy.
4. Apply. Migrations + seed run automatically on first boot; the app binds `$PORT`.

Notes:
- Free web service sleeps after 15 min idle (~30–60s cold start) — fine for internal use.
- Render's **free Postgres expires after ~30 days**. For a longer-lived free DB, delete the
  `databases:` block in `render.yaml` and instead set `DATABASE_URL` (Environment tab) to a
  free **Neon** or **Supabase** connection string — the app parses that URL format too.

## Data model
`Users, Stores, Products, Deliveries, DeliveryItems, Payments, Fridges, StockMovements`.
Outstanding per store = Σ delivered − Σ paid. Stock on hand = Σ signed `StockMovements`.
Saving a delivery auto-creates negative stock movements linked to it.
