using System.ComponentModel.DataAnnotations;

namespace HealthAppApi.Models
{
    public class Record
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; }

        public float HeartRate { get; set; }

        [Required]
        [StringLength(50)]
        public string Mood { get; set; }

        [Required]
        [StringLength(8)]
        public string Duration { get; set; }

        [Required]
        public string Exercises { get; set; }

        public DateTime Created_At { get; set; }
    }
}