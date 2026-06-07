using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DkaizaProject.Models
{
    public class Pago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CitaId { get; set; }
        public Cita Cita { get; set; } = null!;

        [MaxLength(100)]
        public string? PreferenceId { get; set; }

        [MaxLength(100)]
        public string? PaymentId { get; set; }

        [MaxLength(100)]
        public string ExternalReference { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoTotal { get; set; }

        [MaxLength(50)]
        public string? Metodo { get; set; }

        public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaPago { get; set; }

        [MaxLength(50)]
        public string? NumeroOperacion { get; set; }

        public bool Validado { get; set; } = false;

        public DateTime? FechaValidacion { get; set; }

        public int? ValidadoPorClienteId { get; set; }


        [MaxLength(50)]
public string? CuponCodigo { get; set; }

[Column(TypeName = "decimal(10,2)")]
public decimal MontoDescuento { get; set; } = 0;
    }

    public enum EstadoPago
    {
        Pendiente,
        Aprobado,
        Rechazado,
        Cancelado
    }
}
