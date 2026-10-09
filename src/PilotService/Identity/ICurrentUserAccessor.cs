namespace PilotService.Identity;

/// <summary>
/// Scoped access to the acting employee resolved for the current request. The value is set
/// once by <see cref="ActingIdentityEndpointFilter"/>; handlers inject this abstraction and
/// never re-read the request header, so there is exactly one place that decides who is
/// calling.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// The resolved acting employee, or <c>null</c> when the request reached an endpoint that
    /// does not require an identity (the switcher list).
    /// </summary>
    CurrentUser? User { get; }

    /// <summary>
    /// Returns the resolved acting employee for an endpoint that requires one.
    /// </summary>
    /// <returns>The acting employee.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when no identity has been resolved, which means the endpoint was mapped outside
    /// the filtered <c>/api</c> group or was marked as allowing unidentified callers.
    /// </exception>
    CurrentUser Require();
}
