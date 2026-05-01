using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class Servicio
{
    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    [Required, Range(10, 480)]
    public int DuracionHoras { get; set; }

    [Range(0, 10000)]
    public decimal Precio { get; set; }

    public bool Activo { get; set; } = true;

    public byte[]? ImagenBytes { get; set; }
        
    public string? ImagenContentType { get; set; } // Tipo MIME de la imagen

    // Relación con categoría
    public int? CategoriaServicioId { get; set; }
    public CategoriaServicio? Categoria { get; set; }

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}

}