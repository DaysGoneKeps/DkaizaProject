using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class Notificacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
 
        // A qué cliente pertenece
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
 
        // Cita relacionada
        public int CitaId { get; set; }
        public Cita Cita { get; set; } = null!;
 
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
 
        // Si el cliente ya la leyó (para marcar como vista)
        public bool Leida { get; set; } = false;
 
        // Si ya fue procesada (confirmó o canceló) — no vuelve a aparecer
        public bool Procesada { get; set; } = false;
 
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
 
        // Fecha en que el cliente confirmó o canceló (para auditoría)
        public DateTime? FechaAccion { get; set; }
 
        // "Confirmada" | "Cancelada" | null
        public string? AccionRealizada { get; set; }
    }
}