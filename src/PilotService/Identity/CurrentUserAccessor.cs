using System;

namespace PilotService.Identity;

/// <summary>
/// Scoped implementation of <see cref="ICurrentUserAccessor"/>. It is a holder and nothing
/// more: <see cref="ActingIdentityEndpointFilter"/> populates it after it has resolved the
/// employee from the database, and handlers only read it.
/// </summary>
/// <remarks>
/// Registered as a scoped service under both this type and <see cref="ICurrentUserAccessor"/>
/// so the filter can write through the concrete type while handlers depend on the interface,
/// with both resolving to the same instance for the lifetime of one request.
/// </remarks>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    /// <inheritdoc />
    public CurrentUser? User { get; private set; }

    /// <summary>
    /// Records the acting employee resolved for this request.
    /// </summary>
    /// <param name="user">The employee the request is acting as.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an identity has already been set for this request, which would mean two
    /// filters were deciding who the caller is.
    /// </exception>
    public void Set(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (User is not null)
        {
            throw new InvalidOperationException(
                "The acting identity has already been resolved for this request and cannot be replaced.");
        }

        User = user;
    }

    /// <inheritdoc />
    public CurrentUser Require()
    {
        return User ?? throw new InvalidOperationException(
            "No acting identity has been resolved for this request. Endpoints that read the current user must be mapped inside the /api group and must not be marked as allowing unidentified callers.");
    }
}
