using System.ComponentModel.DataAnnotations;

namespace PilotService;

/// <summary>
/// A worked example of the shape every request model is expected to follow:
/// validation lives on the model, not in the handler.
/// </summary>
public sealed class GreetingRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "name must not be empty")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "name must be 1 to 80 characters")]
    public string Name { get; init; } = string.Empty;
}

public sealed record GreetingResponse(string Greeting);
