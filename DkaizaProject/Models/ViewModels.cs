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
}

public class MisCitasViewModel
{
    public List<Cita> Proximas { get; set; } = new();
    public List<Cita> Historial { get; set; } = new();
}

}