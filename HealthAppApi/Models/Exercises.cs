using System.ComponentModel.DataAnnotations;

namespace HealthAppApi.Models
{
    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        public int RecordId { get; set; }

        [Required]
        public string ExerciseName { get; set; }

        [Required]
        public string Metric { get; set; }

        [Required]
        public string Value { get; set; }

        [Required]
        public string Unit { get; set; }

        public Record Record { get; set; }
    }
}