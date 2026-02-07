using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEat.Backend.Models
{
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        public int IdBitacora { get; set; }
        public DateTime Fecha { get; set; }
        public string? Usuario { get; set; }
        public string? Accion { get; set; }
        public string? Entidad { get; set; }
        public int IdEntidad { get; set; }
        public string? Descripcion { get; set; }
        public int? IdEstadoPedido { get; set; }
        public string? Ip { get; set; }

        [ForeignKey("IdEstadoPedido")]
        public virtual EstadoPedido? EstadoPedido { get; set; }
    }
}