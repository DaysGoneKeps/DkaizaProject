using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class CategoriaServicio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public string? Icono { get; set; } // Clase de FontAwesome (ej: "fa-cut")

        public bool Activo { get; set; } = true;

        public int Orden { get; set; } = 0; // Para ordenar categorías

        // Relación con servicios
        public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    }
}