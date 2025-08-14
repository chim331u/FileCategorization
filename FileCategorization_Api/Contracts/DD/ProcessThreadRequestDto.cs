namespace FileCategorization_Api.Contracts.DD;

/// <summary>
/// Request DTO for processing thread by URL
/// </summary>
public class ProcessThreadRequestDto
{
    /// <summary>
    /// The thread URL to process
    /// </summary>
    public string ThreadUrl { get; set; } = string.Empty;
}