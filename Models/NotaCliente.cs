using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class NotaCliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
 
        /// <summary>Cliente al que pertenece la nota</summary>
        [Required]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
 
        /// <summary>Estilista que registró la nota</summary>
        [Required]
        public int EstilistaId { get; set; }
        public Estilista Estilista { get; set; } = null!;
 
        /// <summary>Contenido de la nota (observación de preferencia)</summary>
        [Required, MaxLength(1000)]
        public string Contenido { get; set; } = string.Empty;
 
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}