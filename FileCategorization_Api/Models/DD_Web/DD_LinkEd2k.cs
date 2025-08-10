using System.ComponentModel.DataAnnotations;


namespace FileCategorization_Api.Models.DD_Web;

public class DD_LinkEd2k: BaseEntity
{
    [Key]
    public int Id { get; set; }
    public string Ed2kLink { get; set; }
    public string Title { get; set; }

    public bool IsNew { get; set; }

    public bool IsUsed { get; set; }
    // public int ThreadsId { get; set; } // Required foreign key property
    public DD_Threads Threads { get; set; }  // Required reference navigation to principal
}