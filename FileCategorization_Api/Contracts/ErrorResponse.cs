namespace FileCategorization_Api.Contracts;

/// <summary>
/// Represents an error response returned by the API.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Gets or sets the title of the error.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code associated with the error.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the detailed error message.
    /// </summary>
    public string? Message { get; set; }
}