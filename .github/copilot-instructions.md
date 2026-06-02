# Copilot Instructions for MyPo

## Build & Test Commands

```bash
# Build the entire solution
dotnet build MyPo.sln

# Build a specific project
dotnet build MyPo.Api/MyPo.Api.csproj

# Run tests (CI uses dotnet 8.x/9.x/10.x matrix)
dotnet test MyPo.sln

# Run a single test by filter
dotnet test MyPo.sln --filter "FullyQualifiedName~YourTestName"

# Run the Blazor host (main entry point)
dotnet run --project MyPo.Blazor/MyPo.Blazor/MyPo.Blazor.csproj

# Docker build (multi-stage, .NET 8 Alpine)
docker build -t mypo .
```

## Architecture

This is a .NET 8 solution with a Blazor Server+WASM frontend and multiple API backends, using Entity Framework Core with PostgreSQL (and InMemory for dev/test).

### Project Dependency Layers

```
MyPo.Blazor (host)
├── MyPo.Blazor.Client (WASM entry)
│   ├── MyPo.Blazor.App (shared UI pages/services)
│   └── MyPo.Blazor.Portfolio.App (portfolio UI module)
├── MyPo.Api (core API: auth, users)
├── MyPo.Portfolio.Api (portfolio API: AI, finance)
└── MyPo.Libs (utilities: Clavis, Opurator, StringExtensions)

MyPo.Shared.Api (base controller, DB bootstrap, Redis cache)
├── MyPo.Shared (identity models, JWT, external login, cache)
│   └── MyPo.Shared.EF (generic EF repository, identity DbContext)

MyPo.Portfolio.Shared (portfolio EF models/repos)
├── MyPo.Shared + MyPo.Shared.EF
```

### Key Architectural Patterns

- **Bootstrap pattern**: Each project has a `Bootstrap/` folder with setup classes. `Program.cs` calls `AppBootstrapper.Bootstrap(builder)` which loads assemblies from config and runs bootstrappers.
- **Partial controllers**: API controllers are split across multiple partial class files (e.g., `PortfolioController.cs`, `PortfolioController.*.cs`). Each file handles a specific concern.
- **Generic repository**: `GenericDbContextRepository` and `CacheSupportedGenericDbContextRepository` in `MyPo.Shared.EF` provide the base data access layer.
- **Multi-provider DB support**: `DbBootstrapHelper` supports InMemory, SQLite, SQL Server, and PostgreSQL (with connection pooling options). PostgreSQL is the production database.
- **Identity via EF Core Identity**: `IdentityDbContextRepository` extends `IdentityDbContext<MyPoUser, MyPoRole, string>` with custom CRUD and post-fetch enrichment.

### Frontend (Blazor)

- Uses Blazor interactive Server + WebAssembly rendering
- `MyPo.Blazor.App` contains shared pages/layouts/services consumed by both server and WASM
- `MyPo.Blazor.Portfolio.App` is a module for portfolio-specific UI (uses Markdig for Markdown)
- Client-side state uses `Blazored.LocalStorage`

### External Integrations

- Azure OpenAI / Google GenAI (in Portfolio.Api)
- Telegram bot (in Portfolio.Api)
- Microsoft Identity (MSAL) for external auth
- StackExchange Redis for distributed caching
- FinnHub financial data API

## Conventions

- **Target framework**: .NET 8 (`net8.0`) across all projects.
- **Database schemas**: Managed via SQL scripts in `dbschema/pgsql/`. Patches follow naming: `patch_pgsql_{module}-post-{version}.sql`.
- **Config files**: `appsettings.json` variants exist but are `.disabled` in source. Runtime config lives in `config/` directories.
- **Non-root Docker**: Production container runs as `appuser` (non-root).
- **Globals pattern**: Several projects have a `Globals.cs` file for shared constants/state (e.g., a "ready" flag set after background bootstrap tasks complete).
- **Controller auth**: Portfolio/finance controllers use `[Authorize]` and validate resource ownership via `IIdentityRepository` + current user lookup.
- **Release process**: Uses semantic-release (`.semrelease/`). Releases are triggered by closing PRs to the `release` branch. Release notes go in `.semrelease/this_release`.

## Database

- **Production**: PostgreSQL
- **Dev/Test**: InMemory (default in Docker/dev) or SQLite
- **Schemas**: `dbschema/pgsql/schema_pgsql_identity.sql` and `schema_pgsql_portfolio.sql`
- **Migrations**: Manual SQL patch files, not EF migrations

## CI/CD

- **CI** (`.github/workflows/ci.yaml`): Builds and tests across dotnet 8.x/9.x/10.x, validates Dockerfile with health check
- **Release** (`.github/workflows/release.yaml`): Semrelease → changelog → Docker build/push → deploy to Azure Container Apps
- **CodeQL**: C# security analysis on push/PR/schedule
- **Dependabot**: Auto-merges after CI passes on `main`
