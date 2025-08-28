using System.ComponentModel.DataAnnotations;

namespace FileCategorization_App.Data
{
    public class Configs : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsDev { get; set; }

    }
}
