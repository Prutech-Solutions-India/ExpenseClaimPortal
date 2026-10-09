# Pilot template - .NET 8 + React

A whole product in one repository: an ASP.NET Core 8 Web API, a React + Vite
frontend, and one deployment that carries both. This is the seed the Agentic
SDLC Delivery System generates a `dotnet-react` project from.

Most enterprise requirements need a screen, an API and somewhere to keep the
data. On a backend-only template the agents can only deliver a third of that,
and the UI has to be raised as a separate requirement against a separate
repository - which splits the traceability chain the pipeline exists to keep
intact. This template exists so a .NET shop does not have to make that trade.

## Layout

```
index.html                     shell, loads src/web/main.tsx
package.json                   frontend dependencies and scripts
vite.config.ts                 dev proxy, build output, vitest config
tsconfig.json                  '@/*' resolves to src/web/*
src/PilotService/              ASP.NET Core 8 minimal API
src/PilotService/wwwroot/      built bundle (generated, git-ignored)
src/web/                       React application
tests/PilotService.Tests/      xUnit: unit, integration, smoke
tests/web/                     Vitest: unit, integration, smoke
```

The agents may only write under `src/`, `tests/` and `docs/`, which is why the
frontend lives at `src/web` rather than at the repository root. Everything
outside those prefixes - the manifests, the CI definitions, the shell - belongs
to the template and cannot be rewritten by a model.

## How the two layers join

The frontend calls the API at relative paths, never at an absolute URL:

* In development, `npm run dev` proxies `/api` and `/health` to
  `http://localhost:5080`, where `dotnet run` binds.
* In production, `vite build` writes the bundle into
  `src/PilotService/wwwroot/` and ASP.NET serves it. One process, one artifact,
  one commit - the UI can never be a different version from the API behind it.

Routing follows from that:

* `/health` - the platform's probe, matched exactly.
* `/api/*` - every endpoint the browser calls. An unknown one returns a 404
  problem document, not a page of HTML.
* everything else - the shell, so the client-side router can render it,
  including its own not-found page.

## Running it

```bash
npm ci
dotnet restore

# two terminals
dotnet run --project src/PilotService --urls http://localhost:5080
npm run dev
```

## Tests

```bash
npm run test:unit
npm run test:integration

# The .NET integration suite asserts that the shell is served from wwwroot, so
# the bundle has to exist before it runs. CI does this for you.
npx vite build
dotnet test --filter "Category!=Smoke"
```

Smoke suites on both sides skip when `BASE_URL` is unset rather than passing
against nothing, and the deploy workflows fail if the environment URL is
missing so a promotion cannot report success without having been checked.

## Dependencies

The coding agent cannot edit `package.json` or the `.csproj`; it names what a
story needs and the orchestrator writes the manifest, additions only. NuGet
additions are supported today. npm additions are not, so anything the frontend
needs beyond React, React Router and Testing Library has to be added here
first.
