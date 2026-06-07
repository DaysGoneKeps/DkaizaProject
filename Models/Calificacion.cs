using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class Calificacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CitaId { get; set; }
        public Cita Cita { get; set; } = null!;

        public int EstilistaId { get; set; }
        public Estilista Estilista { get; set; } = null!;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [Range(1, 5)]
        public int Estrellas { get; set; }

        [MaxLength(250)]
        public string? Comentario { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}