using System.ComponentModel.DataAnnotations;

using PilotService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();

// One place turns an unhandled failure into a response. Without it ASP.NET
// returns a bare 500 with no body, which tells the caller nothing and leaves
// nothing to correlate against the log.
app.UseExceptionHandler();
app.UseStatusCodePages();

// The React bundle is built into wwwroot by `npm run build` and served by this
// same process, so the UI and the API deploy as one artifact and can never
// drift to different commits.
app.UseDefaultFiles();
app.UseStaticFiles();

// Outside the /api group and matched exactly, so the platform's probe and the
// client-side router never collide over it.
app.MapGet("/health", () =>
{
    var build = BuildInfo.FromEnvironment();
    return Results.Ok(new { status = "ok", version = build.Version, commit = build.Commit });
});

// Every endpoint the browser calls lives under /api. The prefix is what lets
// the SPA fallback below tell "a page the router should render" from "an
// endpoint that does not exist".
var api = app.MapGroup("/api");

api.MapGet("/health", () =>
{
    var build = BuildInfo.FromEnvironment();
    return Results.Ok(new { status = "ok", version = build.Version, commit = build.Commit });
});

api.MapPost("/greetings", (GreetingRequest request) =>
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, context, results, validateAllProperties: true))
    {
        return Results.ValidationProblem(results.ToDictionary(
            result => result.MemberNames.FirstOrDefault() ?? "request",
            result => new[] { result.ErrorMessage ?? "invalid" }));
    }

    return Results.Created($"/api/greetings/{request.Name}", new GreetingResponse($"Hello, {request.Name}."));
});

// An unknown /api path is a caller mistake and must say so in the same problem
// format as every other error. Registered before the file fallback because
// otherwise it would be answered with a page of HTML and a 200, and a fetch()
// would fail on JSON.parse somewhere far away from the cause.
app.MapFallback("/api/{**slug}", () => Results.Problem(
    title: "No such endpoint.",
    statusCode: StatusCodes.Status404NotFound));

// Anything else that is not a file on disk is a client-side route, so the
// shell is returned and React decides what to render - including its own
// not-found page.
app.MapFallbackToFile("index.html");

app.Run();

// WebApplicationFactory needs a named entry point to boot the application in
// process, and minimal APIs do not generate a public one on their own.
public partial class Program;
