# Mace Hub

A home-network-only platform for hosting personal tools and automating tasks.
Single-user, no authentication, http only — runs on an always-on Ubuntu laptop and
is reached over the local network. Built on ASP.NET Core MVC with a vertical-slice
architecture, htmx, PostgreSQL (EF Core), Hangfire, and a Vite/Tailwind/daisyUI
frontend.

> This repository is currently the **scaffold**: a runnable foundation that proves
> every architectural convention with one disposable `Example` slice. See
> [`SCAFFOLD.md`](SCAFFOLD.md) for what was built and [`CLAUDE.md`](CLAUDE.md) for
> the conventions.

## Prerequisites

- **.NET 10 SDK** (for local build/run and EF migrations)
- **Docker** + Docker Compose (for the full app + Postgres stack)
- **Node.js 22+** and npm (for the Vite/Tailwind frontend build)

## First run (Docker — full stack)

```bash
cp .env.example .env          # then edit .env and fill in real values
docker compose up -d --build  # builds the frontend + app image, starts db + web
```

Then open:

- `http://localhost:8080/` — the Dashboard (hub front door)
- `http://localhost:8080/example` — the Example slice (add an item → htmx row swap)
- `http://localhost:8080/hangfire` — the Hangfire dashboard (no auth, by design)

Postgres data persists in the named volume `macehub-db-data` across
`docker compose down && up`. `docker compose down -v` deletes the volume (and data).

## Local development (without Docker)

You need a reachable PostgreSQL. The dev connection string in
`appsettings.Development.json` points at `localhost:5432` (db/user/pass `macehub`);
the quickest way to get one is to run just the database from compose:

```bash
docker compose up -d db
```

Then, in `MaceHub.Web/`:

```bash
npm install        # first time / after package changes
npm run build      # compile Tailwind/daisyUI CSS + TS → wwwroot/dist
# (or `npm run dev` to rebuild on change)
```

And from the repo root:

```bash
dotnet tool restore                 # restores the pinned dotnet-ef tool
dotnet run --project MaceHub.Web    # auto-applies EF migrations at startup
```

The app reaches the database via `ConnectionStrings:Default` — overridden by the
`ConnectionStrings__Default` environment variable in Docker.

## Frontend notes

- **htmx** is loaded as a pinned-version **CDN `<script>` tag** in
  `Common/Views/Shared/_Layout.cshtml` (not bundled through Vite). The browser needs
  internet access to fetch it; vendor it locally if the box must run fully offline.
- Vite compiles only TypeScript and the Tailwind v4 / daisyUI 5 CSS into
  `wwwroot/dist/{main.js,main.css}` (fixed filenames; `_Layout` references them
  directly). daisyUI is configured CSS-first in `src/main.css`, dark theme only.

## Tests

```bash
dotnet test
```

## Adding a feature

Create a new slice folder under `MaceHub.Web/Features/` (controller + views + its own
EF access), following the conventions in [`CLAUDE.md`](CLAUDE.md). Views resolve
automatically from the controller's namespace via `FeatureViewLocationExpander` — no
per-controller configuration. **Delete the `Example` slice once the first real
feature exists.**
