using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class Cliente
{
    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public bool EsAdmin { get; set; } = false;

    public bool EsEstilista { get; set; } = false;

    public bool EsRecepcionista { get; set; } = false;

    public int? EstilistaId { get; set; }
    public Estilista? Estilista { get; set; }

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public string NombreCompleto => $"{Nombre} {Apellido}";
}

}