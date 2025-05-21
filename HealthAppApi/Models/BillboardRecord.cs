using System.ComponentModel.DataAnnotations;

namespace HealthAppApi.Models
{
    public class BillboardRecord
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SongTitle { get; set; }

        [Required]
        [StringLength(100)]
        public string Artist { get; set; }

        [Required]
        public int ChartRank { get; set; }

        [Required]
        public int StarNumber { get; set; }

        public DateTime Updated_At { get; set; }
    }
}