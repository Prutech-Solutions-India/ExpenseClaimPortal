using Microsoft.AspNetCore.Builder;

namespace PilotService.Identity;

/// <summary>
/// Single source of truth for how the browser tells the server which employee it is acting
/// as. The frontend constant in <c>src/web/lib/api.ts</c> must match <see cref="HeaderName"/>
/// exactly - two spellings of the same header is how an identity quietly stops arriving.
/// </summary>
public static class ActingIdentity
{
    /// <summary>Request header carrying the acting employee's identifier.</summary>
    public const string HeaderName = "X-Acting-Employee-Id";
}

/// <summary>
/// Endpoint metadata marking an endpoint inside the <c>/api</c> group as reachable without an
/// acting identity. Only the switcher list carries it: a caller has to be able to see the
/// staff list before it can choose who to be.
/// </summary>
public sealed class AllowUnidentifiedMetadata
{
    /// <summary>The single shared instance; the type carries no state.</summary>
    public static AllowUnidentifiedMetadata Instance { get; } = new();

    private AllowUnidentifiedMetadata()
    {
    }
}

/// <summary>
/// Convenience conventions for attaching <see cref="AllowUnidentifiedMetadata"/> to an endpoint.
/// </summary>
public static class AllowUnidentifiedExtensions
{
    /// <summary>
    /// Exempts the endpoint from the acting-identity requirement enforced by
    /// <see cref="ActingIdentityEndpointFilter"/>.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder being configured.</typeparam>
    /// <param name="builder">The endpoint to mark.</param>
    /// <returns>The same builder, so conventions can be chained.</returns>
    public static TBuilder AllowUnidentified<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(AllowUnidentifiedMetadata.Instance);
        return builder;
    }
}
