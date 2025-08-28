namespace FileCategorization_App.Data.DTOs;

public class ThreadsDto
{
    public int Id { get; set; }
    public string? MainTitle { get; set; }
    public string? Type { get; set; }
    public int LinksNumber { get; set; }
    public bool NewLinks { get; set; }
    
}