# CI dependency notes - SCRUM-2065

Two CI jobs failed on the first push of this branch. They have different causes and
different owners, and this note records both so neither is quietly forgotten.

## 1. `dotnet restore` failed on npm package ids (fixed)

```
error NU1101: Unable to find package react-router-dom. No packages exist with this id in source(s): nuget.org
error NU1102: Unable to find package vitest with version (>= 4.1.11)
```

`react-router-dom` and `vitest` are npm packages. They were requested through the
pipeline's dependency channel, which - because this repository's declared language is
C# - writes every request into the .NET project files as a `PackageReference`. NuGet
then looked for an npm id on nuget.org and failed, taking both `PilotService.csproj`
and `PilotService.Tests.csproj` down with it before any compilation happened.

Neither request was necessary. Both packages are already present in the committed
`package.json` and `package-lock.json`:

- `react-router-dom` backs `src/web/App.tsx`, `src/web/main.tsx` and the
  `MemoryRouter` in `tests/web/integration/App.test.tsx`.
- `vitest` is the test runner for everything under `tests/web/`.

The `npm ci` step in the same workflow run installed them successfully, so the
frontend imports resolve exactly as written.

**Rule for this repository:** only NuGet package ids belong in the dependency
request. Frontend packages are managed in `package.json`, which is a dependency
manifest and therefore not edited from a story branch.

The NuGet packages this story genuinely needs, and the only ones requested now:

| Package | Version | Used by |
| --- | --- | --- |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | `PilotService.Data.AppDbContext`, `UseNpgsql` in `Program.cs` |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.31 | the explicit `InitialCreate` migration and its snapshot |
| `Testcontainers.PostgreSql` | 3.10.0 | `tests/PilotService.Tests/Integration/PostgresFixture.cs` when no `ConnectionStrings__Default` is supplied |

The two EF Core packages are versioned independently; the numbers above are the
ones the stack mandates and neither one is derived from the other. No Npgsql
runtime package is named directly - the EF Core provider brings the one it needs.

## 2. `npm audit` advisories (not fixable from this branch)

```
7 vulnerabilities (3 moderate, 2 high, 2 critical)
```

| Advisory | Reached through |
| --- | --- |
| `brace-expansion` (moderate, DoS) | `@typescript-eslint/typescript-estree` -> eslint |
| `react-router` (moderate, open redirect / constructor injection) | `react-router-dom` |
| `source-map-js` (high, event-loop DoS) | `vite` -> postcss |
| `tinypool` (critical, prototype pollution to RCE) | `vitest` |

Every one of these is a transitive dependency of the pre-existing frontend
manifest. This story adds no npm package, and the fixes `npm audit` proposes
(`npm audit fix --force`, installing `react-router-dom@7.18.4` and
`vitest@4.1.11`) are breaking changes to `package.json` and `package-lock.json`.

Those files are dependency manifests and sit outside the paths this story is
allowed to modify, and routing the bump through the pipeline's dependency channel
is what caused failure 1 above. The remediation therefore belongs to a dedicated
dependency-maintenance change that owns the manifest, the lockfile and the
migration work the major-version bumps imply:

- `react-router-dom` 6 -> 7 changes the data-router APIs; `App.tsx`, `main.tsx`
  and `tests/web/integration/App.test.tsx` would need reviewing together so the
  shell keeps exactly one router.
- `vitest` 3 -> 4 changes config and mocking defaults, which affects every file
  under `tests/web/`.

Until that change lands, the audit job reports pre-existing debt rather than
anything introduced by SCRUM-2065.
