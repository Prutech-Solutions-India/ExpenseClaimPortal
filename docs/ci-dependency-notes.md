# CI dependency notes - SCRUM-2065

Two CI jobs failed on this branch. They have different causes and different owners.
One was mine and is fixed; the other is pre-existing manifest debt that cannot be
paid from a story branch. Both are recorded here so neither is quietly forgotten.

## 1. `dotnet restore` failed on npm package ids (fixed)

```
error NU1101: Unable to find package react-router-dom. No packages exist with this id in source(s): nuget.org
error NU1102: Unable to find package vitest with version (>= 4.1.11)
  - Found 3 version(s) in nuget.org [ Nearest version: 1.0.0 ]
```

### What happened

`react-router-dom` and `vitest` are npm packages. They were named in the pipeline's
dependency request, and because this repository's declared language is C# the
channel writes every request into the .NET project files as a `PackageReference`.
NuGet then went looking for an npm id on nuget.org, found nothing (or, for
`vitest`, found an unrelated id whose highest version is 1.0.0), and failed both
`src/PilotService/PilotService.csproj` and `tests/PilotService.Tests/PilotService.Tests.csproj`
before a single line was compiled. Nothing downstream of restore ran, so this
failure says nothing about the code itself.

### Why neither request was necessary

Both packages are already present in the committed `package.json` and
`package-lock.json`. The `npm ci` step in the same workflow run reported
"added 291 packages" and exited zero, so the frontend imports resolve exactly as
written:

- `react-router-dom` backs `src/web/App.tsx` (`Routes`, `Route`, `Link`),
  `src/web/main.tsx` (`BrowserRouter`) and the `MemoryRouter` in
  `tests/web/integration/App.test.tsx`.
- `vitest` is the test runner for everything under `tests/web/`.

### The fix

The two npm ids are no longer requested. Only NuGet ids are, and the project files
are left to the pipeline - they are dependency manifests and are not edited from a
story branch.

**Standing rule for this repository:** only NuGet package ids belong in the
dependency request. Frontend packages are managed in `package.json`. If a
frontend package is genuinely missing, that is a manifest change and needs its own
change, not a line smuggled through the backend channel.

### The NuGet packages this story genuinely needs

| Package | Version | Used by |
| --- | --- | --- |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | `PilotService.Data.AppDbContext`, `UseNpgsql` in `Program.cs` |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.31 | the explicit `InitialCreate` migration, its designer file and the model snapshot |
| `Testcontainers.PostgreSql` | 3.10.0 | `tests/PilotService.Tests/Integration/PostgresFixture.cs`, when the environment supplies no `ConnectionStrings__Default` |

The two EF Core packages are versioned independently: 8.0.11 is the provider,
8.0.31 is the design-time tooling, and neither number is derived from the other.
No Npgsql runtime package is named directly - the EF Core provider brings the one
it needs, and naming it separately is how the two end up disagreeing.

## 2. `npm audit` advisories (pre-existing, not fixable from this branch)

```
7 vulnerabilities (3 moderate, 2 high, 2 critical)
```

| Advisory | Severity | Reached through |
| --- | --- | --- |
| `brace-expansion` - DoS via uncontrolled recursion | moderate | `@typescript-eslint/typescript-estree` -> eslint |
| `react-router` - open redirect via backslash in `<Link>`/`useNavigate` | moderate | `react-router-dom` |
| `react-router` - constructor injection via `deserializeErrors()` in SSR hydration | moderate | `react-router-dom` |
| `source-map-js` - event-loop DoS via indexed section offsets | high | `vite` -> postcss |
| `tinypool` - prototype pollution gadget to RCE | critical | `vitest` |

Every one is a transitive dependency of the pre-existing frontend manifest.
SCRUM-2065 adds no npm package; this set of advisories is identical before and
after the branch.

### Why it is not fixed here

The remediation npm proposes is `npm audit fix --force`, which installs
`react-router-dom@7.18.4` and `vitest@4.1.11` - breaking major bumps that rewrite
`package.json` and `package-lock.json`. Those are dependency manifests and sit
outside the paths this story may modify, and routing the bump through the
pipeline's dependency channel is exactly what produced failure 1 above.

The alternatives that would silence the job are all worse than the debt:

- Dropping `react-router-dom` and hand-rolling navigation contradicts the stack
  convention (React Router for navigation) and would mean two places deciding what
  a URL renders.
- Downgrading or replacing `vitest` would take every test under `tests/web/` with
  it, which is deleting the evidence rather than fixing the fault.

### Who owns it

A dedicated dependency-maintenance change that owns the manifest, the lockfile and
the migration work the major versions imply:

- `react-router-dom` 6 -> 7 changes the data-router APIs. `src/web/App.tsx`,
  `src/web/main.tsx` and `tests/web/integration/App.test.tsx` need reviewing
  together so the shell keeps exactly one router and one identity provider.
- `vitest` 3 -> 4 changes config and mocking defaults (`vi.stubGlobal`,
  `vi.hoisted`, environment resolution), which touches every file under
  `tests/web/`.
- The `brace-expansion` and `source-map-js` advisories clear with a plain
  `npm audit fix` once the lockfile is owned by that change.

Until it lands, the audit job is reporting debt that predates this story rather
than anything SCRUM-2065 introduced.
