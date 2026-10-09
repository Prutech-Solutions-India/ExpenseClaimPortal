namespace PilotService.Contracts;

/// <summary>
/// The machine-readable error payload returned by every rejection the API makes, so a
/// caller can branch on <see cref="Code"/> rather than on prose or on an HTML page.
/// Serialised with the default camelCase policy as <c>{"code":...,"message":...}</c>.
/// </summary>
/// <param name="Code">Stable identifier for the failure, for example <c>acting_identity_missing</c>.</param>
/// <param name="Message">Human-readable explanation intended for a developer or a log line.</param>
public sealed record ApiError(
    string Code,
    string Message)
{
    /// <summary>No acting employee identifier was supplied, or it was not an integer. Paired with 401.</summary>
    public const string ActingIdentityMissingCode = "acting_identity_missing";

    /// <summary>The supplied acting employee identifier matched no employee row. Paired with 403.</summary>
    public const string ActingIdentityUnknownCode = "acting_identity_unknown";

    /// <summary>No endpoint under <c>/api</c> matched the request. Paired with 404.</summary>
    public const string NotFoundCode = "not_found";

    /// <summary>Creates the payload returned when the acting identity header is absent or malformed.</summary>
    public static ApiError ActingIdentityMissing() => new(
        ActingIdentityMissingCode,
        "An acting employee identifier is required. Send a positive integer employee id in the X-Acting-Employee-Id header.");

    /// <summary>Creates the payload returned when the acting identity header names no known employee.</summary>
    /// <param name="employeeId">The identifier that was supplied but could not be resolved.</param>
    public static ApiError ActingIdentityUnknown(int employeeId) => new(
        ActingIdentityUnknownCode,
        $"Employee {employeeId} does not exist, so the request cannot be attributed to an acting identity.");

    /// <summary>Creates the payload returned by the <c>/api</c> fallback so an unmatched API path never receives HTML.</summary>
    /// <param name="path">The request path that matched no endpoint.</param>
    public static ApiError NotFound(string path) => new(
        NotFoundCode,
        $"No API endpoint matches '{path}'.");
}
