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

        // ✅ NUEVO: Página para explorar servicios
        public async Task<IActionResult> Servicios()
        {
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
        var slots = GetSlots(e, servicio.DuracionHoras, citasDelDia.Where(c => c.EstilistaId == e.Id).ToList(), fechaDate);
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

            var slots = GetSlots(estilista, servicio.DuracionHoras, citas, fechaDate);
            return Json(slots);
        }

        // POST /Appointments/Crear - Valida y deriva al checkout de pago
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearCitaDto dto)
        {
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
                decimal descuento = Math.Min(dto.Descuento, montoTotal);
                decimal montoFinal = Math.Round(montoTotal - descuento, 2);

                var pago = new Pago
                {
                    CitaId = cita.Id,
                    ExternalReference = Guid.NewGuid().ToString("N"),
                    Monto = 0m,
                    MontoTotal = montoFinal,
                    Metodo = "Efectivo",
                    Estado = EstadoPago.Pendiente,
                    CuponCodigo = dto.CuponCodigo,
                    MontoDescuento = descuento
                };
                _db.Pagos.Add(pago);

                // Incrementar uso del cupón si aplica
                if (!string.IsNullOrEmpty(dto.CuponCodigo))
                {
                    var cupon = await _db.Cupones
                        .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == dto.CuponCodigo.ToUpper());
                    if (cupon != null) cupon.UsosActuales++;
                }

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
                ExternalReference = Guid.NewGuid().ToString("N"),
                CuponCodigo = dto.CuponCodigo,
                Descuento = dto.Descuento
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
                Proximas = citas.Where(c =>
    c.Fecha.Date >= hoy &&
    c.Estado != EstadoCita.Cancelada &&
    c.Estado != EstadoCita.Completada &&
    c.Estado != EstadoCita.Pagada)
    .OrderBy(c => c.Fecha).ToList(),

Historial = citas.Where(c =>
    c.Fecha.Date < hoy ||
    c.Estado == EstadoCita.Cancelada ||
    c.Estado == EstadoCita.Completada ||
    c.Estado == EstadoCita.Pagada)
    .ToList()
            };
            return View(vm);
        }


        // POST /Appointments/ValidarCupon
[HttpPost]
public async Task<IActionResult> ValidarCupon([FromBody] ValidarCuponDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Codigo))
        return Json(new { success = false, message = "Ingresa un código de cupón." });

    var cupon = await _db.Cupones
        .FirstOrDefaultAsync(c =>
            c.Codigo.ToUpper() == dto.Codigo.ToUpper().Trim() &&
            c.Activo);

    if (cupon == null)
        return Json(new { success = false, message = "Cupón inválido o no existe." });

    if (cupon.FechaExpiracion.HasValue && cupon.FechaExpiracion.Value < DateTime.Today)
        return Json(new { success = false, message = "Este cupón ha expirado." });

    if (cupon.UsosActuales >= cupon.UsoMaximo)
        return Json(new { success = false, message = "Este cupón ha alcanzado su límite de usos." });

    decimal descuento = 0;
    if (cupon.EsPorcentaje)
        descuento = Math.Round(dto.MontoOriginal * cupon.PorcentajeDescuento / 100, 2);
    else
        descuento = Math.Min(cupon.MontoDescuento, dto.MontoOriginal);

    decimal montoFinal = dto.MontoOriginal - descuento;

    return Json(new
    {
        success = true,
        message = "Cupón aplicado correctamente.",
        descuento,
        montoFinal,
        descripcion = cupon.Descripcion ?? "",
        esPorcentaje = cupon.EsPorcentaje,
        porcentaje = cupon.PorcentajeDescuento
    });
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


        // GET /Appointments/CalificarServicio/5
public async Task<IActionResult> CalificarServicio(int citaId)
{
    if (CurrentClienteId == null)
        return RedirectToAction("Login", "Account");

    var cita = await _db.Citas
        .Include(c => c.Servicio)
        .Include(c => c.Estilista)
        .FirstOrDefaultAsync(c =>
            c.Id == citaId &&
            c.ClienteId == CurrentClienteId.Value &&
            (c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Pagada));

    if (cita == null)
{
    TempData["Error"] = $"Cita no encontrada o estado incorrecto para citaId={citaId}, clienteId={CurrentClienteId}";
    return RedirectToAction("MisCitas");
}

    // Verificar que no haya sido calificada ya
    var yaCalificada = await _db.Calificaciones
        .AnyAsync(c => c.CitaId == citaId);

    if (yaCalificada)
        return RedirectToAction("MisCitas");

    return View(cita);
}

// POST /Appointments/GuardarCalificacion
[HttpPost]
public async Task<IActionResult> GuardarCalificacion([FromBody] GuardarCalificacionDto dto)
{
    if (CurrentClienteId == null)
        return Json(new { success = false, message = "Debes iniciar sesión." });

    var cita = await _db.Citas
        .FirstOrDefaultAsync(c =>
            c.Id == dto.CitaId &&
            c.ClienteId == CurrentClienteId.Value &&
            (c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Pagada));

    if (cita == null)
        return Json(new { success = false, message = "Cita no encontrada." });

    var yaCalificada = await _db.Calificaciones
        .AnyAsync(c => c.CitaId == dto.CitaId);

    if (yaCalificada)
        return Json(new { success = false, message = "Esta cita ya fue calificada." });

    if (dto.Estrellas < 1 || dto.Estrellas > 5)
        return Json(new { success = false, message = "Selecciona entre 1 y 5 estrellas." });

    var calificacion = new Calificacion
    {
        CitaId = dto.CitaId,
        EstilistaId = cita.EstilistaId,
        ClienteId = CurrentClienteId.Value,
        Estrellas = dto.Estrellas,
        Comentario = dto.Comentario?.Trim().Length > 250
            ? dto.Comentario.Trim().Substring(0, 250)
            : dto.Comentario?.Trim(),
        FechaCreacion = DateTime.Now
    };

    _db.Calificaciones.Add(calificacion);
    await _db.SaveChangesAsync();

    // Recalcular promedio del estilista de forma asíncrona
    var promedio = await _db.Calificaciones
        .Where(c => c.EstilistaId == cita.EstilistaId)
        .AverageAsync(c => (double)c.Estrellas);

    return Json(new
    {
        success = true,
        message = "Muchas gracias por calificar nuestro servicio.",
        nuevaPromedio = Math.Round(promedio, 1)
    });
}



        // ── Helpers ──────────────────────────────────────────────────────────────
        private static List<HorarioDisponibleDto> GetSlots(Estilista e, int duracion, List<Cita> citas, DateTime fecha)
        {
            var slots = new List<HorarioDisponibleDto>();
            var ahora = DateTime.Now;
            bool esHoy = fecha.Date == ahora.Date;

            for (int h = e.HoraInicioTrabajo; h + duracion <= e.HoraFinTrabajo; h++)
            {
                int fin = h + duracion;
                bool enDescanso = h < e.HoraFinDescanso && fin > e.HoraInicioDescanso;
                bool enCita     = citas.Any(c => c.HoraInicio < fin && c.HoraFin > h);
                // Si es hoy, bloquear si la hora de inicio ya pasó (o está dentro de los próximos 0 min)
                bool yaPaso     = esHoy && h <= ahora.Hour;

                bool disponible = !enDescanso && !enCita && !yaPaso;

                slots.Add(new HorarioDisponibleDto
                {
                    HoraInicio  = h,
                    HoraFin     = fin,
                    Disponible  = disponible,
                    Label       = $"{h:D2}:00 - {fin:D2}:00"
                });
            }
            return slots;
        }

        // GET /Appointments/Estilistas - Listado público de estilistas con su promedio de calificaciones
        public async Task<IActionResult> Estilistas()
        {
            var estilistas = await _db.Estilistas
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            var ids = estilistas.Select(e => e.Id).ToList();
            var stats = await _db.Calificaciones
                .Where(c => ids.Contains(c.EstilistaId))
                .GroupBy(c => c.EstilistaId)
                .Select(g => new { EstilistaId = g.Key, Promedio = g.Average(x => (double)x.Estrellas), Total = g.Count() })
                .ToListAsync();

            ViewBag.Stats = stats.ToDictionary(s => s.EstilistaId, s => (s.Promedio, s.Total));
            return View(estilistas);
        }

        // GET /Appointments/PerfilEstilista/5 - Perfil público del estilista con sus reseñas
        public async Task<IActionResult> PerfilEstilista(int id, int pagina = 1)
        {
            var estilista = await _db.Estilistas.FirstOrDefaultAsync(e => e.Id == id && e.Activo);
            if (estilista == null)
                return RedirectToAction("Estilistas");

            const int porPagina = 10;

            var totalComentarios = await _db.Calificaciones
                .CountAsync(c => c.EstilistaId == id);

            var comentarios = await _db.Calificaciones
                .Include(c => c.Cliente)
                .Where(c => c.EstilistaId == id)
                .OrderByDescending(c => c.FechaCreacion)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            double promedio = totalComentarios > 0
                ? await _db.Calificaciones
                    .Where(c => c.EstilistaId == id)
                    .AverageAsync(c => (double)c.Estrellas)
                : 0;

            ViewBag.Estilista = estilista;
            ViewBag.Promedio = Math.Round(promedio, 1);
            ViewBag.TotalCalificaciones = totalComentarios;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalComentarios / porPagina);

            return View(comentarios);
        }
    }
}