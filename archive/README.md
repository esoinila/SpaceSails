# Archive

Code that's out of the default build/CI path but kept around (with full git history) in case
it's worth resurrecting.

## What's here

- **`SpaceSails.Server/`** — ASP.NET Core host that serves the same Blazor client plus a
  SignalR hub (`GameHub`), an authoritative session (`SessionHost`), and the departures-board
  API. Lets multiple browser tabs/machines join one shared system (`?mp=1`, warp follows the
  slowest crew member).
- **`SpaceSails.Server.Tests/`** — xUnit tests for the hub and traffic endpoint.
- **`Dockerfile`** — single-container build (client + server in one image) for Azure Container
  Apps.
- **`deploy.yml`** — GitHub Actions workflow (`workflow_dispatch`) that built the Docker image
  via `az acr build` and pushed it to Azure Container Apps.
- **`run-server.ps1`, `run-server-debug.ps1`** — thin wrappers around the repo-root `run.ps1
  -Server` for launching the multiplayer host locally.

## Why archived

Multiplayer was never actually playtested — the fun that's been proven out so far is the
single-player navigation and orbital plotting loop. Rather than keep maintaining an untested
SignalR/ACA path, the game now ships as a static site on GitHub Pages
(`.github/workflows/pages.yml`, single-player only). Archiving here (instead of deleting) keeps
the code and its history one `git mv` away from coming back.

The client (`src/SpaceSails.Client`) still contains its multiplayer code paths (`?mp=1`) —
they're just inert on a static host with no hub to connect to. Stripping that client-side code
is tracked separately (plan §PR-8, "Rig for silent running") so it doesn't conflict with
everything else being built in parallel.

## How to resurrect

1. Move the code back:
   ```bash
   git mv archive/SpaceSails.Server src/SpaceSails.Server
   git mv archive/SpaceSails.Server.Tests tests/SpaceSails.Server.Tests
   git mv archive/Dockerfile Dockerfile
   git mv archive/deploy.yml .github/workflows/deploy.yml
   git mv archive/run-server.ps1 run-server.ps1
   git mv archive/run-server-debug.ps1 run-server-debug.ps1
   ```
2. Revert the relative-path fixups made when these were archived:
   - `src/SpaceSails.Server/SpaceSails.Server.csproj` — `ProjectReference` paths go back to
     `..\SpaceSails.Core\...` etc. (one `..\` shorter).
   - `tests/SpaceSails.Server.Tests/SpaceSails.Server.Tests.csproj` — `ProjectReference` back to
     `..\..\src\SpaceSails.Server\SpaceSails.Server.csproj`.
   - `run-server.ps1` / `run-server-debug.ps1` — back to `$PSScriptRoot/run.ps1` (they live at
     the repo root again).
   - `run.ps1` — `-Server` should point at `src/SpaceSails.Server` again.
3. Re-add both projects to `SpaceSails.slnx` (`/src/` and `/tests/` folders).
4. `dotnet run -c Release --project src/SpaceSails.Server` should now serve the client + hub at
   `http://localhost:5295`; `dotnet test SpaceSails.slnx` picks the hub tests back up.
5. Re-enable `deploy.yml` needs the same Azure secrets/variables documented in its header
   comment (`AZURE_CREDENTIALS`, `ACR_NAME`, `AZURE_RESOURCE_GROUP`, `ACA_APP_NAME`,
   `ACA_ENVIRONMENT`).
