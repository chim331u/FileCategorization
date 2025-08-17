using System.ComponentModel.DataAnnotations;


namespace FileCategorization_Api.Domain.Entities.FileCategorization;

public class Configs : BaseEntity
{
    [Required]
    public string? Key { get; set; }
    [Required]
    public string? Value { get; set; }
    public bool IsDev { get; set; }

}