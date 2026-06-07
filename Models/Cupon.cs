using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
namespace DkaizaProject.Models
{
    public class Cupon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Codigo { get; set; } = string.Empty;

        // Porcentaje de descuento (ej: 10 = 10%)
        [Range(0, 100)]
        public decimal PorcentajeDescuento { get; set; }

        // Monto fijo de descuento (ej: 20 = S/20)
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoDescuento { get; set; }

        public bool EsPorcentaje { get; set; } = true;

        public bool Activo { get; set; } = true;

        public DateTime? FechaExpiracion { get; set; }

        public int UsoMaximo { get; set; } = 100;

        public int UsosActuales { get; set; } = 0;

        [MaxLength(200)]
        public string? Descripcion { get; set; }
    }
}