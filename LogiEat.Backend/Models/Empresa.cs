using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("Empresa")]
    public class Empresa
    {
        [Key]
        [Column("id_empresa")]
        public int IdEmpresa { get; set; }

        [Required]
        [StringLength(100)]
        [Column("nombre_empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [StringLength(150)]
        [Column("direccion")]
        public string Direccion { get; set; } = string.Empty;
    }
}