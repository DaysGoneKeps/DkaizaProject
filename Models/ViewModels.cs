using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
 
        public bool Recordarme { get; set; }
    }
 
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El apellido es requerido")]
        public string Apellido { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El teléfono es requerido")]
        public string Telefono { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
 
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
 
    public class ReservaViewModel
    {
        public List<Servicio> Servicios { get; set; } = new();
        public List<Estilista> Estilistas { get; set; } = new();
    }
 
    public class HorarioDisponibleDto
    {
        public int HoraInicio { get; set; }
        public int HoraFin { get; set; }
        public bool Disponible { get; set; }
        public string Label { get; set; } = string.Empty;
    }
 
    public class EstilistasDisponiblesDto
    {
        public int EstilistaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Horario { get; set; } = string.Empty;
        public string Descanso { get; set; } = string.Empty;
        public int HorariosLibres { get; set; }
    }
 
    public class CrearCitaDto
    {
        public int ServicioId { get; set; }
        public int EstilistaId { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public int HoraInicio { get; set; }
        public string? Notas { get; set; }
        public string? MetodoPago { get; set; }
        public int? CitaId { get; set; }
        public bool Reprogramando { get; set; }
    }
 
    public class ReservaPendiente
    {
        public int ClienteId { get; set; }
        public int ServicioId { get; set; }
        public int EstilistaId { get; set; }
        public DateTime Fecha { get; set; }
        public int HoraInicio { get; set; }
        public int HoraFin { get; set; }
        public string? Notas { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
    }

    public class ResumenMensual
    {
        public int    Mes            { get; set; }
        public string NombreMes      { get; set; } = string.Empty;
        public int    TotalAtendidas { get; set; }
        public string ServicioTop    { get; set; } = string.Empty;
        public string EstilistaTop   { get; set; } = string.Empty;
    }
 
    public class CheckoutViewModel
    {
        public Servicio Servicio { get; set; } = null!;
        public Estilista Estilista { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int HoraInicio { get; set; }
        public int HoraFin { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoSenal { get; set; }
        public int PorcentajeSenal { get; set; }
        public string Currency { get; set; } = "PEN";
        public bool ConfigurationOk { get; set; }
    }
 
    public class MisCitasViewModel
    {
        public List<Cita> Proximas { get; set; } = new();
        public List<Cita> Historial { get; set; } = new();
    }
 
    public class PerfilViewModel
    {
        public int Id { get; set; }
 
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El apellido es requerido")]
        [Display(Name = "Apellido")]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Correo electrónico")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        [MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;
 
        [Display(Name = "Nueva contraseña")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string? NuevaPassword { get; set; }
 
        [Display(Name = "Confirmar contraseña")]
        [DataType(DataType.Password)]
        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmarPassword { get; set; }
 
        public DateTime FechaRegistro { get; set; }
 
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
 
    // ============================================================
    // HU-10 — Historial de Clientes
    // ============================================================
 
    public class ClienteHistorialResumen
    {
        public int ClienteId { get; set; }
 
        public string NombreCompleto { get; set; } = string.Empty;
 
        public string Email { get; set; } = string.Empty;
 
        /// <summary>
        /// Citas con estado Completada o Pagada.
        /// </summary>
        public int Asistencias { get; set; }
 
        /// <summary>
        /// Citas con estado Cancelada.
        /// </summary>
        public int Cancelaciones { get; set; }
 
        /// <summary>
        /// Citas con fecha pasada que no fueron completadas ni canceladas.
        /// </summary>
        public int Ausencias { get; set; }
 
        /// <summary>
        /// Alta (>=8) | Media (>=3) | Baja (<3)
        /// </summary>
        public string Frecuencia { get; set; } = string.Empty;
 
        /// <summary>
        /// True si el admin lo marcó manualmente o si tiene frecuencia Alta.
        /// </summary>
        public bool EsVip { get; set; }
 
        /// <summary>
        /// Fecha de la última cita completada/pagada. Null si nunca asistió.
        /// </summary>
        public DateTime? UltimaVisita { get; set; }
    }
}