using System.ComponentModel.DataAnnotations;


namespace FileCategorization_Api.Domain.Entities.DD_Web;

public class DD_Threads : BaseEntity
{
    public string Url { get; set; }
    public string? MainTitle { get; set; }
    public string? Type { get; set; }
    
    public ICollection<DD_LinkEd2k?> LinkEd2Ks { get; } // Collection navigation containing dependents

}