using System.ComponentModel.DataAnnotations;

namespace HealthAppApi.Models
{
    public class RecordWrapper
    {
        [Required]
        public Record Record { get; set; }
    }
}