namespace FileCategorization_Api.Common;

/// <summary>
/// Represents an exception that is thrown when a file detail with a specified ID does not exist.
/// </summary>
public class FilesDetailDoesNotExistException : Exception
{
    /// <summary>
    /// Gets the ID of the file detail that does not exist.
    /// </summary>
    private int Id { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesDetailDoesNotExistException"/> class.
    /// </summary>
    /// <param name="id">The ID of the file detail that does not exist.</param>
    public FilesDetailDoesNotExistException(int id) 
        : base($"Files detail with id {id} does not exist")
    {
        this.Id = id;
    }
}