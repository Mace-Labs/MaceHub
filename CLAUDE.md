# Mace Hub

A home-network-only platform for hosting personal tools and automating tasks.
Runs on an always-on Ubuntu laptop, accessed over the local network only (http,
no public exposure). Single-user, no authentication.

This file is persistent context for Claude Code. Read it before working on the
project. Keep it accurate as decisions change.

---

## Tech stack

**Platform:** `net10.0`, nullable reference types enabled, implicit usings
enabled.

**Backend**
- .NET 10, ASP.NET Core **MVC** (primary surface)
- **Minimal API** endpoints only where a JSON API is genuinely needed (e.g. an
  endpoint consumed by client-side TypeScript rather than rendered as HTML).
  Such endpoints live in the same `Features/{Feature}/` folder as the rest of the
  slice, exposed via a per-slice `MapXyz(this IEndpointRouteBuilder)` extension
  method that `Program.cs` calls. They do NOT go in a separate top-level folder —
  the slice stays the unit of cohesion whether it renders HTML or returns JSON.
- **Vertical slice architecture**, organised under `Features/`. No mediator
  pattern (no MediatR). Controllers act as the handler (controller-as-handler).
- **Validation:** data annotations for the simple read/config inputs the
  day-one features need. **FluentValidation is deferred** — add it (one validator
  per slice, integrated into the MVC model validation pipeline) when the first
  slice with non-trivial user input arrives. Don't scaffold it ahead of a need.

**Frontend**
- **Razor** views and partial views — server-rendered HTML. First-party MVC
  Razor only; no third-party view libraries (RazorSlices was considered and
  rejected to avoid single-maintainer coupling in the rendering pipeline).
- **htmx** for interactions and partial-fragment swaps
- **htmx SSE extension** — deferred; add when a feature genuinely streams to the
  browser (e.g. live LLM token streaming or live job status). None of the
  day-one features need it.
- **TypeScript**, compiled/bundled with **Vite**, for genuine client-side logic
  only (not for primary rendering — that's server-side)
- **Tailwind CSS (v4)**, with **daisyUI 5** (Tailwind plugin) as the component
  layer for cohesive styling — semantic component classes (`btn`, `card`, etc.),
  pure CSS, no JavaScript, so it does not conflict with htmx. **Tailwind v4 +
  daisyUI 5 are CSS-first: daisyUI is loaded and themed via a CSS `@plugin`
  directive in the entry stylesheet, NOT a `tailwind.config.js` `daisyui: {}`
  block.** Configured with a **fixed dark theme, no theme switcher** (single-user
  personal hub) by restricting enabled themes in that directive:
  `@plugin "daisyui" { themes: dark --default; }`. Swapping to another dark theme
  (`night`, `business`, `dracula`, etc.) is a one-line change there. Use
  daisyUI 5 class names (`card-border`, not the removed `card-bordered`).
- Shared/reusable UI lives in `Common/` (Razor partials/tag helpers) composing
  daisyUI classes; slices compose these, they do NOT restyle per-slice. This
  keeps styling cohesive across vertical slices. Write a custom partial only
  where daisyUI lacks a needed component.
- **Alpine.js** — deferred; add only if a real client-state need arises

**Data**
- **PostgreSQL** (plain — no pgvector day one)
- **EF Core** with the Npgsql provider. EF Core migrations for schema management.
- **pgvector is deferred**, not part of the day-one build. No current feature
  needs semantic similarity search — the first features (SEO/GEO opportunity
  finder, multi-site metrics dashboard, bin-collection notifier) are all API
  ingestion, structured metrics, scraping, and LLM-over-freshly-fetched-data.
  **Trigger to add it:** when the SEO tool needs semantic retrieval over existing
  content (e.g. "find what I've already written that's related before generating
  a new article" / RAG over the Bet Shrew corpus). At that point: switch the
  Postgres image to `pgvector/pgvector`, enable the `vector` extension via
  migration, register vector support on the Npgsql data source
  (`dataSourceBuilder.UseVector()` + `.UseVector()` on the EF options — the
  migration alone is not enough at runtime), and use the `Pgvector.Vector` type
  for embedding columns, not `float[]`.

**Background jobs**
- **Hangfire**, using `Hangfire.PostgreSql` against the same Postgres instance
  (one datastore, no separate Redis/SQL Server).

**AI**
- **Slices depend on an owned `ILlmClient` interface, never on a provider SDK
  directly.** Define a small interface (the handful of methods features actually
  need — a completion call now; a streaming call later) and one implementation,
  `AnthropicLlmClient`, that wraps the provider SDK internally. This is the one
  deliberate abstraction in the project: it keeps the provider's blast radius to a
  single class so swapping providers later means writing one new implementation,
  not touching any slice. (Consistent with the minimal-abstraction rule elsewhere
  — this seam earns its place because provider-swappability is an explicit goal;
  do NOT extend it into a multi-provider configurable abstraction until a second
  provider actually exists.)
- **Anthropic is the first provider.** `AnthropicLlmClient` uses the
  **`Anthropic.SDK`** community NuGet package internally. It's an acceptable
  dependency precisely because it's already isolated behind `ILlmClient` — if the
  package goes stale it can be swapped for a thin `HttpClient` wrapper inside that
  one class. The SDK type must not leak into slice code, controllers, or view
  models.
- Day-one LLM calls are plain request/response (the SEO tool is a batch flow).
  Pin the model string in configuration, not hardcoded in the client or handlers.
  If/when streaming to the browser is added, the streaming method on `ILlmClient`
  relays the provider's streamed completion over the htmx SSE extension (server
  reads the token stream, pushes SSE events, htmx swaps them in) — deferred until
  a feature needs it.

**Testing**
- **xUnit** unit tests only (no SpecFlow/BDD for this project). Keep tests
  focused on slice handler logic and validators. A test project may be added, but
  do not scaffold heavyweight integration/BDD tooling.

**Infrastructure**
- **Docker Compose**. All long-running services use `restart: unless-stopped`
  so they survive reboots/power cuts.
- Always-on Ubuntu laptop, home-network only, http.
- **Secrets**: `.env` file, gitignored. Injected via Compose as environment
  variables, read through .NET configuration. No cloud secret store (the box is
  deliberately self-contained — do NOT introduce Azure Key Vault or similar).

**Security posture (explicit, settled assumption)**
- No authentication, http only, home-LAN only. This means **anything on the home
  network can read and write everything**, including the Hangfire dashboard and
  all data. This is a deliberate, accepted trade-off for a single-user personal
  box — do NOT "helpfully" add auth, HTTPS, or lock down the dashboard without an
  explicit decision to change the threat model.

---

## Commands

Literal invocations. Adjust paths/project names if they differ once scaffolded.

**Run / build the app**
```bash
dotnet build                                  # build solution
dotnet run --project MaceHub.Web              # run app locally (no Docker)
```

**EF Core migrations**
```bash
dotnet ef migrations add <Name> --project MaceHub.Web   # create a migration
dotnet ef database update --project MaceHub.Web         # apply manually (rarely
                                                        # needed; app auto-migrates
                                                        # at startup)
dotnet ef migrations remove --project MaceHub.Web       # undo last unapplied
```

**Frontend (Vite + Tailwind + TypeScript)**
```bash
npm install            # first time / after package changes
npm run dev            # vite build --watch during development
npm run build          # production build → wwwroot/dist
```

**Docker Compose**
```bash
docker compose up -d              # start app + Postgres (detached)
docker compose up -d --build      # rebuild images then start
docker compose logs -f web        # follow app logs
docker compose down               # stop (data persists in the named volume)
docker compose down -v            # stop AND delete the volume (destroys data)
```

**Testing**
```bash
dotnet test                       # run xUnit tests
```

---

## Architecture conventions

### Vertical slice layout
Everything for one feature lives in one folder under `Features/`. A slice owns
its controller, request/view models, its validator (once FluentValidation is
introduced — see Tech stack; data annotations until then), EF data access for
its own needs, and its Razor views/partials.

**Sub-features.** A feature may contain **one level** of sub-feature folders,
each with its own controller. The view-location expander is namespace-driven, so
a controller in `MaceHub.Web.Features.Seo.Opportunities` resolves its views from
`Features/Seo/Opportunities/` automatically — no per-controller config, no areas.
A sub-feature can also use a partial owned by its parent feature (the expander
probes one level up).

**Guardrail: do not nest more than one level deep.** VSA's whole point is that a
slice is a self-contained unit of behaviour. If a sub-feature wants its own
sub-features, that's a signal it's really a bounded context that should be
*promoted to a top-level slice* (or eventually its own project), not nested
further. One level of nesting is an organisational convenience for genuinely
related sub-tools (e.g. a feature's list view vs its config vs its history);
two levels is a design smell to stop and reconsider.

```
Features/
  Example/                    // DISPOSABLE scaffold slice — delete once the
    ExampleController.cs      // first real feature exists. Proves the wiring
    Index.cshtml              // (view-location expander + full page), ONE htmx
    _ExampleRow.cshtml        // fragment swap, and ONE level of sub-feature
    Details/                  // nesting. Validation and EF patterns establish
      DetailsController.cs    // themselves in the first real slice, not here.
      Index.cshtml
  Dashboard/                  // the hub's front door: lists/links to features
    DashboardController.cs
    Index.cshtml
Infrastructure/
  FeatureViewLocationExpander.cs   // namespace-driven; finds views under Features/
  MaceHubDbContext.cs
  Llm/
    ILlmClient.cs                  // owned interface; slices depend on this
    AnthropicLlmClient.cs          // ONLY file referencing the Anthropic.SDK type
  ...cross-cutting filters, config
Common/
  ...genuinely shared view models, layout, _ViewImports, _ViewStart
```

### Rules
- **Controller-as-handler.** The controller action is the handler. It calls the
  EF context / validator directly within the slice. Do NOT introduce generic
  repository or service layers. Resist extracting shared abstractions until two
  slices genuinely duplicate real logic — high cohesion, minimal abstraction.
- **View resolution** is handled by a custom `IViewLocationExpander`
  (`FeatureViewLocationExpander`) registered against `RazorViewEngineOptions`. It
  is **namespace-driven**: a controller's namespace under `...Features.` gives the
  view path, so both top-level features (`Features/{Feature}/`) and one-level
  sub-features (`Features/{Feature}/{SubFeature}/`) resolve with no per-controller
  config. It also resolves feature-local partials, a one-level-up probe (so a
  sub-feature can use its parent's partials), and a shared location for cross-slice
  layouts/partials. This is the one place framework defaults are overridden; it is
  standard practice for VSA-on-MVC, not a hack.
- **Full-page vs fragment actions.** Controllers will have both. Convention:
  - `return View(vm)` for full page loads.
  - `return PartialView("_Name", vm)` for htmx fragment swaps.
  - Partial view files are underscore-prefixed (e.g. `_ExampleRow.cshtml`).
- **Validation.** Server-owned, as the single source of truth. Data annotations
  for day-one needs; once FluentValidation is introduced, its validator lives in
  the slice. Invalid model state surfaces back to the view (full page) or as an
  error fragment (htmx).
- **C# style.** Models use `init` accessors and the `required` keyword for
  required properties. Prefer modern .NET 10 idioms.
- **Disposable Example slice.** `Features/Example/` exists only to prove the
  wiring in a runnable form — the view-location expander resolves a feature-local
  view, one full-page action renders, and one htmx fragment swap works. It does
  NOT demonstrate validation or EF access; those patterns establish themselves in
  the first real slice. **Deleting Example is the first task after the scaffold is
  verified** — before building the first real feature. Do not copy it as a
  template or let it ship.

### Comments
- **Default to no comment.** A comment is a maintained artifact with an ongoing
  cost; a stale comment is worse than none. Only keep one that explains something
  the code cannot — a non-obvious *why* (trade-off, workaround, external
  constraint/gotcha) or a warning whose violation causes a real bug.
- Do **not** write comments that restate the code, narrate the change or task
  ("step 1", "now we…", "proves the wiring"), or duplicate CLAUDE.md/docs.
  Task-relative notes go stale — "step 1" may not be step 1 after the next edit.
- The test: **would it still be true and useful two years from now, after the
  code around it has been refactored?** If not, omit it. Removing low-value
  comments is a normal part of reviewing a diff.

### Frontend asset pipeline (Vite → Razor)
- Vite compiles TypeScript and Tailwind CSS. **Output goes to a fixed path under
  `wwwroot`** (e.g. `wwwroot/dist/`) that `_Layout.cshtml` references directly.
- Keep it simple: a production `npm run build` (or `vite build`) emits hashed/
  predictable assets into `wwwroot`; `_Layout` links them with plain `<script>`/
  `<link>` tags. Avoid a Vite dev-server proxy / HMR integration unless a real
  need arises — for a personal hub, build-and-reference is enough and removes a
  moving part. A `vite build --watch` during development is fine.
- The Docker image build runs the Vite/Tailwind build as a stage so the
  published image contains compiled assets (no Node needed at runtime).
- If a manifest is used for cache-busting, `_Layout` reads it; otherwise a fixed
  filename is acceptable for a single-user app. State which approach is in use
  here once decided.

### Database migrations
- **Strategy:** EF Core migrations, applied automatically at app startup
  (`db.Database.Migrate()` in `Program.cs`). This is a deliberate choice for a
  single-user, single-instance personal app — the app and DB restart together,
  there is no multi-instance race, and self-updating schema on launch is the
  right ergonomic. Wrap the call so a failed migration logs clearly and halts
  startup rather than half-applying.
- Every schema change gets its own migration (`dotnet ef migrations add X`).
- **Never use `EnsureCreated()`** — it does not coexist with migrations and is a
  foot-gun. Migrations are the only schema mechanism in this project.
- **Ordering:** migrations MUST complete before the Hangfire server starts
  processing jobs — Hangfire's own schema and any job-touched tables must exist
  first. In `Program.cs`, run `Migrate()` before starting/registering the
  Hangfire server. (Matters more if the Hangfire server is ever split into its
  own process; pinning the order now prevents a subtle startup race.)

---

## Project goals / non-goals

- **Goal:** a useful, self-contained personal tools hub built efficiently on
  patterns the author already knows (MVC + vertical slices), extended with htmx
  and Postgres.
- **Non-goal:** public exposure, multi-user, authentication, cloud dependency.
- **Deferred (not day one):** pgvector / semantic search (add when the SEO tool
  needs retrieval over existing content), htmx SSE streaming (add when a feature
  streams to the browser), FluentValidation (add at the first slice with
  non-trivial input), authentication (not needed), backups (will add a scheduled
  `pg_dump`, likely as a Hangfire job), HTTPS/local TLS, Alpine.js.

---

## Notes for Claude Code
- When adding a feature, create a new slice folder under `Features/` following
  the layout above. Do not scatter its pieces across top-level
  `Controllers/`/`Views/`/`Models/` folders.
- Keep the `.env`-based secret flow; never hardcode the Anthropic key or DB
  credentials, and never commit `.env`.
- Prefer Razor partials returned to htmx over building JSON-and-render-in-TS,
  unless a feature specifically calls for a client-side JSON API.
