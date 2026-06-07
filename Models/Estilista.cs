using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
   public class Estilista
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Especialidad { get; set; }

    [Range(0, 23)]
    public int HoraInicioTrabajo { get; set; } = 10;

    [Range(0, 23)]
    public int HoraFinTrabajo { get; set; } = 22;

    [Range(0, 23)]
    public int HoraInicioDescanso { get; set; } = 12;

    [Range(0, 23)]
    public int HoraFinDescanso { get; set; } = 13;

    public bool Activo { get; set; } = true;

    // Foto del estilista
    public byte[]? FotoBytes { get; set; }
    public string? FotoContentType { get; set; }

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public string HorarioTexto =>
        $"{HoraInicioTrabajo:D2}:00 - {HoraFinTrabajo:D2}:00";

    public string DescansoTexto =>
        $"{HoraInicioDescanso:D2}:00 - {HoraFinDescanso:D2}:00";


    public ICollection<Calificacion> Calificaciones { get; set; } = new List<Calificacion>();
}
}