using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DkaizaProject.Data;
using DkaizaProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

 
namespace DkaizaProject.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public NotificacionesController(ApplicationDbContext db) => _db = db;
 
        private int? ClienteId => HttpContext.Session.GetInt32("ClienteId");
 
        // -------------------------------------------------------
        // GET /Notificaciones
        // Página completa con todas las notificaciones del cliente
        // -------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            if (ClienteId == null)
                return RedirectToAction("Login", "Account");
 
            var notifs = await _db.Notificaciones
                .Include(n => n.Cita)
                    .ThenInclude(c => c.Servicio)
                .Include(n => n.Cita)
                    .ThenInclude(c => c.Estilista)
                .Where(n => n.ClienteId == ClienteId.Value)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
 
            return View(notifs);
        }
 
        // -------------------------------------------------------
        // GET /Notificaciones/Pendientes
        // AJAX: devuelve notificaciones no procesadas (para el popup)
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Pendientes()
        {
            if (ClienteId == null)
                return Json(new { success = false });
 
            var notifs = await _db.Notificaciones
                .Include(n => n.Cita)
                    .ThenInclude(c => c.Servicio)
                .Include(n => n.Cita)
                    .ThenInclude(c => c.Estilista)
                .Where(n =>
                    n.ClienteId == ClienteId.Value &&
                    !n.Procesada)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
 
            // NO marcamos como leídas aquí.
            // El badge persiste hasta que el usuario confirme o cancele.
 
            var resultado = notifs.Select(n => new
            {
                n.Id,
                n.Titulo,
                n.Mensaje,
                n.Leida,
                n.Procesada,
                FechaCreacion = n.FechaCreacion.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                Cita = new
                {
                    n.Cita.Id,
                    Servicio  = n.Cita.Servicio.Nombre,
                    Estilista = n.Cita.Estilista.Nombre,
                    Fecha     = n.Cita.Fecha.ToString("dd/MM/yyyy"),
                    HoraInicio = $"{n.Cita.HoraInicio:D2}:00",
                    HoraFin    = $"{n.Cita.HoraFin:D2}:00",
                    Estado    = n.Cita.Estado.ToString()
                }
            });
 
            return Json(new { success = true, notificaciones = resultado });
        }
 
        // -------------------------------------------------------
        // GET /Notificaciones/Contador
        // AJAX ligero: sólo devuelve la cantidad no leída (para la campana)
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Contador()
        {
            if (ClienteId == null)
                return Json(new { count = 0 });
 
            var count = await _db.Notificaciones
                .CountAsync(n =>
                    n.ClienteId == ClienteId.Value &&
                    !n.Procesada);
 
            return Json(new { count });
        }
 
        // -------------------------------------------------------
        // POST /Notificaciones/Confirmar/5
        // El cliente confirma asistencia desde la notificación
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Confirmar(int id)
        {
            if (ClienteId == null)
                return Json(new { success = false, message = "No autenticado" });
 
            var notif = await _db.Notificaciones
                .Include(n => n.Cita)
                .FirstOrDefaultAsync(n => n.Id == id && n.ClienteId == ClienteId.Value);
 
            if (notif == null)
                return Json(new { success = false, message = "Notificación no encontrada" });
 
            if (notif.Procesada)
                return Json(new { success = false, message = "Ya fue procesada" });
 
            // Confirmar la cita
            notif.Cita.Estado = EstadoCita.Confirmada;
            notif.Procesada = true;
            notif.AccionRealizada = "Confirmada";
            notif.FechaAccion = DateTime.UtcNow;
 
            await _db.SaveChangesAsync();
 
            return Json(new { success = true, message = "¡Cita confirmada! Te esperamos 💆‍♀️" });
        }
 
        // -------------------------------------------------------
        // POST /Notificaciones/Cancelar/5
        // El cliente cancela la cita desde la notificación
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (ClienteId == null)
                return Json(new { success = false, message = "No autenticado" });
 
            var notif = await _db.Notificaciones
                .Include(n => n.Cita)
                .FirstOrDefaultAsync(n => n.Id == id && n.ClienteId == ClienteId.Value);
 
            if (notif == null)
                return Json(new { success = false, message = "Notificación no encontrada" });
 
            if (notif.Procesada)
                return Json(new { success = false, message = "Ya fue procesada" });
 
            // Cancelar la cita (libera el horario automáticamente)
            notif.Cita.Estado = EstadoCita.Cancelada;
            notif.Procesada = true;
            notif.AccionRealizada = "Cancelada";
            notif.FechaAccion = DateTime.UtcNow;
 
            await _db.SaveChangesAsync();
 
            return Json(new { success = true, message = "Cita cancelada. El horario quedó libre." });
        }
 
        // -------------------------------------------------------
        // POST /Notificaciones/MarcarLeida/5
        // Descarta la notificación sin acción (solo la cierra)
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            if (ClienteId == null)
                return Json(new { success = false });
 
            var notif = await _db.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.ClienteId == ClienteId.Value);
 
            if (notif == null)
                return Json(new { success = false });
 
            notif.Leida = true;
            await _db.SaveChangesAsync();
 
            return Json(new { success = true });
        }
    }
}