using System.ComponentModel.DataAnnotations;

namespace HealthAppApi.Models
{
    public class User
    {
        [Required]
        [StringLength(50)]
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        public int Age { get; set; }

        [StringLength(50)]
        public string? Sex { get; set; }

        public DateTime Created_At { get; set; }

        public bool IsAdmin { get; set; }
    }
}