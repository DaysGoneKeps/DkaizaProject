using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class Cita
{
    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    public int EstilistaId { get; set; }
    public Estilista Estilista { get; set; } = null!;

    [Required]
    public DateTime Fecha { get; set; }

    [Range(0, 23)]
    public int HoraInicio { get; set; }

    [Range(1, 23)]
    public int HoraFin { get; set; }

    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;

    [MaxLength(500)]
    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public Pago? Pago { get; set; }
}

public enum EstadoCita
{
    Pendiente,
    Confirmada,
    Cancelada,
    Completada
}
}