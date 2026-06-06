using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Models;
using DkaizaProject.Data;

namespace DkaizaProject.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db) => _db = db;

        private int? CurrentClienteId => HttpContext.Session.GetInt32("ClienteId");

        public const string ReservaPendienteSessionKey = "ReservaPendiente";

        private IActionResult? RedirectIfRestrictedRole()
        {
            if (HttpContext.Session.GetString("EsEstilista") == "True")
                return RedirectToAction("Index", "Estilista");
            if (HttpContext.Session.GetString("EsRecepcionista") == "True")
                return RedirectToAction("Index", "Recepcionista");
            return null;
        }

        // ✅ NUEVO: Página para explorar servicios
        public async Task<IActionResult> Servicios()
        {
            var redirect = RedirectIfRestrictedRole(); if (redirect != null) return redirect;

            var categorias = await _db.CategoriasServicios
                .Include(c => c.Servicios)
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();

            return View(categorias);
        }

        // GET /Appointments/Reservar - REQUIERE LOGIN
        public async Task<IActionResult> Reservar()
        {
            var redirect = RedirectIfRestrictedRole(); if (redirect != null) return redirect;

            if (CurrentClienteId == null)
            {
                var returnUrl = Request.Path + Request.QueryString;
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var vm = new ReservaViewModel
            {
                Servicios = await _db.Servicios.Where(s => s.Activo).ToListAsync(),
                Estilistas = await _db.Estilistas.Where(e => e.Activo).ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
public async Task<IActionResult> EstilistasDisponibles(int servicioId, string fecha)
{
    if (!DateTime.TryParse(fecha, out var fechaDate))
        return Json(new { error = "Fecha inválida" });

    var servicio = await _db.Servicios.FindAsync(servicioId);
    if (servicio == null) return Json(new { error = "Servicio no encontrado" });

    var estilistas = await _db.Estilistas.Where(e => e.Activo).ToListAsync();
    var citasDelDia = await _db.Citas
        .Where(c => c.Fecha.Date == fechaDate.Date && c.Estado != EstadoCita.Cancelada)
        .ToListAsync();

    var result = estilistas.Select(e =>
    {
        var slots = GetSlots(e, servicio.DuracionHoras, citasDelDia.Where(c => c.EstilistaId == e.Id).ToList());
        return new 
        {
            estilistaId = e.Id,
            nombre = e.Nombre,
            especialidad = e.Especialidad ?? "",
            horario = e.HorarioTexto,
            descanso = e.DescansoTexto,
            horariosLibres = slots.Count(s => s.Disponible),
            fotoBase64 = e.FotoBytes != null ? Convert.ToBase64String(e.FotoBytes) : null,
            fotoContentType = e.FotoContentType
        };
    }).ToList();

    return Json(result);
}

        // GET /Appointments/HorariosDisponibles - PERMITE VER SIN LOGIN
        [HttpGet]
        public async Task<IActionResult> HorariosDisponibles(int servicioId, int estilistaId, string fecha)
        {
            if (!DateTime.TryParse(fecha, out var fechaDate))
                return Json(new { error = "Fecha inválida" });

            var servicio = await _db.Servicios.FindAsync(servicioId);
            var estilista = await _db.Estilistas.FindAsync(estilistaId);
            if (servicio == null || estilista == null)
                return Json(new { error = "Datos inválidos" });

            var citas = await _db.Citas
                .Where(c => c.Fecha.Date == fechaDate.Date && c.EstilistaId == estilistaId && c.Estado != EstadoCita.Cancelada)
                .ToListAsync();

            var slots = GetSlots(estilista, servicio.DuracionHoras, citas);
            return Json(slots);
        }

        // POST /Appointments/Crear - Valida y deriva al checkout de pago
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearCitaDto dto)
        {
            if (HttpContext.Session.GetString("EsEstilista") == "True" || HttpContext.Session.GetString("EsRecepcionista") == "True")
                return Json(new { success = false, message = "Tu rol no puede registrar citas desde este flujo." });

            if (CurrentClienteId == null)
                return Json(new { success = false, message = "Debes iniciar sesión para reservar.", requiresLogin = true });

            if (!DateTime.TryParse(dto.Fecha, out var fecha))
                return Json(new { success = false, message = "Fecha inválida." });

            var servicio = await _db.Servicios.FindAsync(dto.ServicioId);
            var estilista = await _db.Estilistas.FindAsync(dto.EstilistaId);
            if (servicio == null || estilista == null)
                return Json(new { success = false, message = "Datos inválidos." });

            int horaFin = dto.HoraInicio + servicio.DuracionHoras;
            // 🔥 REPROGRAMAR CITA
if (dto.Reprogramando && dto.CitaId.HasValue)
{
    var cita = await _db.Citas
        .FirstOrDefaultAsync(c =>
            c.Id == dto.CitaId.Value &&
            c.ClienteId == CurrentClienteId.Value);

    if (cita == null)
    {
        return Json(new
        {
            success = false,
            message = "Cita no encontrada"
        });
    }

    // validar conflicto EXCLUYENDO la misma cita
    var conflictoReprogramacion = await _db.Citas.AnyAsync(c =>
        c.Id != cita.Id &&
        c.Fecha.Date == fecha.Date &&
        c.EstilistaId == dto.EstilistaId &&
        c.Estado != EstadoCita.Cancelada &&
        c.HoraInicio < horaFin &&
        c.HoraFin > dto.HoraInicio);

    if (conflictoReprogramacion)
    {
        return Json(new
        {
            success = false,
            message = "El horario ya fue reservado"
        });
    }

    // 🔥 ACTUALIZAR
    cita.ServicioId = dto.ServicioId;
    cita.EstilistaId = dto.EstilistaId;
    cita.Fecha = fecha;
    cita.HoraInicio = dto.HoraInicio;
    cita.HoraFin = horaFin;

    await _db.SaveChangesAsync();

    return Json(new
    {
        success = true
    });
}

            var conflicto = await _db.Citas.AnyAsync(c =>
                c.Fecha.Date == fecha.Date &&
                c.EstilistaId == dto.EstilistaId &&
                c.Estado != EstadoCita.Cancelada &&
                c.HoraInicio < horaFin &&
                c.HoraFin > dto.HoraInicio);

            if (conflicto)
                return Json(new { success = false, message = "El horario ya fue reservado. Por favor selecciona otro." });

            if (dto.HoraInicio < estilista.HoraFinDescanso && horaFin > estilista.HoraInicioDescanso)
                return Json(new { success = false, message = "El horario coincide con el descanso del estilista." });

            var metodo = (dto.MetodoPago ?? string.Empty).Trim().ToLowerInvariant();

            if (metodo == "efectivo")
            {
                var cita = new Cita
                {
                    ClienteId = CurrentClienteId.Value,
                    ServicioId = dto.ServicioId,
                    EstilistaId = dto.EstilistaId,
                    Fecha = fecha,
                    HoraInicio = dto.HoraInicio,
                    HoraFin = horaFin,
                    Notas = dto.Notas,
                    Estado = EstadoCita.Pendiente
                };
                _db.Citas.Add(cita);
                await _db.SaveChangesAsync();

                decimal montoTotal = Math.Round(servicio.Precio, 2);
                var pago = new Pago
                {
                    CitaId = cita.Id,
                    ExternalReference = Guid.NewGuid().ToString("N"),
                    Monto = 0m,
                    MontoTotal = montoTotal,
                    Metodo = "Efectivo",
                    Estado = EstadoPago.Pendiente
                };
                _db.Pagos.Add(pago);
                await _db.SaveChangesAsync();

                HttpContext.Session.Remove(ReservaPendienteSessionKey);

                return Json(new
                {
                    success = true,
                    metodoPago = "efectivo",
                    mensaje = "Reserva realizada, realice el pago al asistir"
                });
            }

            var pendiente = new ReservaPendiente
            {
                ClienteId = CurrentClienteId.Value,
                ServicioId = dto.ServicioId,
                EstilistaId = dto.EstilistaId,
                Fecha = fecha,
                HoraInicio = dto.HoraInicio,
                HoraFin = horaFin,
                Notas = dto.Notas,
                ExternalReference = Guid.NewGuid().ToString("N")
            };
            HttpContext.Session.SetString(ReservaPendienteSessionKey, JsonSerializer.Serialize(pendiente));

            return Json(new
            {
                success = true,
                metodoPago = "mercadopago",
                redirectUrl = Url.Action("Iniciar", "Payment")
            });
        }

        // GET /Appointments/MisCitas
        public async Task<IActionResult> MisCitas()
        {
            if (CurrentClienteId == null)
                return RedirectToAction("Login", "Account");

            var hoy = DateTime.Today;
            var citas = await _db.Citas
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.ClienteId == CurrentClienteId.Value)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var vm = new MisCitasViewModel
            {
                Proximas = citas.Where(c => c.Fecha.Date >= hoy && c.Estado != EstadoCita.Cancelada).OrderBy(c => c.Fecha).ToList(),
                Historial = citas.Where(c => c.Fecha.Date < hoy || c.Estado == EstadoCita.Cancelada).ToList()
            };
            return View(vm);
        }

        // POST /Appointments/Cancelar/5
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (CurrentClienteId == null)
                return Json(new { success = false });

            var cita = await _db.Citas.FindAsync(id);
            if (cita == null || cita.ClienteId != CurrentClienteId.Value)
                return Json(new { success = false, message = "Cita no encontrada." });

            if (cita.Fecha < DateTime.Now)
                return Json(new { success = false, message = "No puedes cancelar una cita pasada." });

            cita.Estado = EstadoCita.Cancelada;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static List<HorarioDisponibleDto> GetSlots(Estilista e, int duracion, List<Cita> citas)
        {
            var slots = new List<HorarioDisponibleDto>();
            for (int h = e.HoraInicioTrabajo; h + duracion <= e.HoraFinTrabajo; h++)
            {
                int fin = h + duracion;
                bool enDescanso = h < e.HoraFinDescanso && fin > e.HoraInicioDescanso;
                bool enCita = citas.Any(c => c.HoraInicio < fin && c.HoraFin > h);
                bool disponible = !enDescanso && !enCita;

                slots.Add(new HorarioDisponibleDto
                {
                    HoraInicio = h,
                    HoraFin = fin,
                    Disponible = disponible,
                    Label = $"{h:D2}:00 - {fin:D2}:00"
                });
            }
            return slots;
        }
    }
}