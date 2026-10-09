using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using PilotService.Contracts;
using PilotService.Data;
using PilotService.Endpoints;
using PilotService.Health;
using PilotService.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

// The connection string comes from configuration only - the ConnectionStrings__Default
// environment variable. When it is absent we fall back to a deliberately unreachable DSN
// rather than to a usable literal: the host still starts, and /health says plainly that the
// database cannot be reached. A credential baked into source is a credential that leaks.
const string unreachablePlaceholderConnectionString =
    "Host=database-not-configured.invalid;Port=5432;Database=pilot;Username=unset;Password=unset;Timeout=2;Command Timeout=2";

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = unreachablePlaceholderConnectionString;
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// One instance per request, reachable through both the concrete type (the identity filter
// writes to it) and the interface (handlers read from it).
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor>(services => services.GetRequiredService<CurrentUserAccessor>());

builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name, tags: new[] { "ready", "db" });

var app = builder.Build();

// Bring the database up to date before serving anything: migrations first, then the
// idempotent seed script. The initialiser never throws - a failure is logged at Critical and
// surfaces through /health, because a host that refuses to start reports nothing at all.
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.RunAsync(scope);
}

// One place turns an unhandled failure into a response. Without it ASP.NET returns a bare
// 500 with no body, which tells the caller nothing and leaves nothing to correlate against
// the log.
app.UseExceptionHandler();

// The React bundle is built into wwwroot by `npm run build` and served by this same process,
// so the UI and the API deploy as one artifact and can never drift to different commits.
app.UseDefaultFiles();
app.UseStaticFiles();

// Mapped outside the /api group and matched exactly, so the platform's probe and the
// client-side router never collide over it. Healthy is 200, anything else 503.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync,
    AllowCachingResponses = false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});

// Every endpoint the browser calls lives under /api. The prefix is what lets the SPA
// fallback below tell "a page the router should render" from "an endpoint that does not
// exist". The filter resolves the acting employee for the whole group, so authorisation is
// decided in one place on the server rather than by whichever control the UI chose to hide.
var api = app.MapGroup("/api")
    .AddEndpointFilter<ActingIdentityEndpointFilter>();

api.MapEmployeeEndpoints()
   .MapIdentityEndpoints();

// An unknown /api path is a caller mistake and must say so in the same machine-readable
// shape as every other rejection. Registered before the file fallback because otherwise it
// would be answered with a page of HTML and a 200, and a fetch() would fail on JSON.parse
// somewhere far away from the cause.
app.MapFallback("/api/{**rest}", (HttpContext context) => Results.Json(
    ApiError.NotFound(context.Request.Path.Value ?? "/api"),
    statusCode: StatusCodes.Status404NotFound));

// Anything else that is not a file on disk is a client-side route, so the React shell is
// returned and React decides what to render - including its own not-found page. There is no
// Razor and no Blazor here: a second way to render a page is how the two halves drift apart.
app.MapFallbackToFile("index.html");

app.Run();

// WebApplicationFactory needs a named entry point to boot the application in process, and
// minimal APIs do not generate a public one on their own.
public partial class Program;
