using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("TipoPago")]
    public class TipoPago
    {
        [Key]
        public int IdTipoPago { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;
    }
}