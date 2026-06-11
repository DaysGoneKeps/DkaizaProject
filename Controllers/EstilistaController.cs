using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Data;
using DkaizaProject.Models;

namespace DkaizaProject.Controllers
{
    public class EstilistaController : Controller
    {
        // ⚠️ DEMO: si está en true, permite iniciar atención cualquier día (no solo el día de la cita).
        // Volver a false para restaurar la regla HU-17 (solo citas de hoy).
        public const bool DEMO_PERMITIR_CUALQUIER_DIA = true;

        private readonly ApplicationDbContext _db;

        public EstilistaController(ApplicationDbContext db) => _db = db;
 
        private bool EsEstilista => HttpContext.Session.GetString("EsEstilista") == "True";
        private int? EstilistaId  => HttpContext.Session.GetInt32("EstilistaId");
 
        private IActionResult? EstilistaOnly()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!EsEstilista || EstilistaId == null)
                return RedirectToAction("Index", "Home");
            return null;
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // HU-19 | GET /Estilista  — Lista de citas asignadas al estilista
        // Muestra solo citas NO canceladas. Las completadas no aparecen como
        // pendientes (criterio HU-18, detalle 2).
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var check = EstilistaOnly();
            if (check != null) return check;
 
            var hoy = DateTime.Today;

            var citas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c =>
                    c.EstilistaId == EstilistaId &&
                    c.Estado != EstadoCita.Cancelada &&
                    c.Estado != EstadoCita.Completada &&
                    c.Fecha.Date >= hoy)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.HoraInicio)
                .ToListAsync();
 
            return View(citas);
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // HU-19 | GET /Estilista/DetalleCita/5
        // Consulta el detalle completo de una cita asignada al estilista.
        // Solo puede ver citas que le pertenezcan (criterio HU-19, detalle 3).
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> DetalleCita(int id)
        {
            var check = EstilistaOnly();
            if (check != null) return check;
 
            var cita = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Include(c => c.Pago)
                .FirstOrDefaultAsync(c => c.Id == id);
 
            if (cita == null) return NotFound();
 
            // HU-19 detalle 3: solo el estilista asignado puede consultar el detalle
            if (cita.EstilistaId != EstilistaId) return Forbid();
 
            return View(cita);
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // HU-17 | POST /Estilista/IniciarAtencion
        // Marca una cita como "En proceso" y registra la hora exacta de inicio.
        //
        // Validaciones:
        //   • El estado debe ser Pendiente (HU-17 detalle 1).
        //   • La cita debe ser del día actual  (HU-17 detalle 4 / tarea T7).
        //   • Solo el estilista asignado puede iniciarla (HU-17 detalle 4 / tarea T6).
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarAtencion(int id)
        {
            var check = EstilistaOnly();
            if (check != null)
                return Json(new { success = false, message = "No autorizado" });
 
            var cita = await _db.Citas.FindAsync(id);
 
            if (cita == null)
                return Json(new { success = false, message = "Cita no encontrada" });
 
            // HU-17 tarea T6: solo el estilista asignado
            if (cita.EstilistaId != EstilistaId)
                return Json(new { success = false, message = "No tienes permiso para iniciar esta cita" });
 
            // HU-17 detalle 1: la cita debe estar en estado Pendiente
            if (cita.Estado != EstadoCita.Pendiente && cita.Estado != EstadoCita.Confirmada)
                return Json(new { success = false, message = "Solo puedes iniciar citas en estado Pendiente" });
 
            // HU-17 tarea T7: la cita debe ser del día actual
            if (!DEMO_PERMITIR_CUALQUIER_DIA && cita.Fecha.Date != DateTime.Today)
                return Json(new { success = false, message = "Solo puedes iniciar citas del día de hoy" });
 
            // HU-17 tarea T2: registrar hora exacta de inicio
            cita.HoraInicioAtencion = DateTime.Now;
 
            // HU-17 detalle 2: cambiar estado a En proceso
            cita.Estado = EstadoCita.EnProceso;
 
            await _db.SaveChangesAsync();
 
            return Json(new
            {
                success        = true,
                horaInicio     = cita.HoraInicioAtencion!.Value.ToString("HH:mm:ss"),
                horaInicioTick = new DateTimeOffset(cita.HoraInicioAtencion.Value).ToUnixTimeMilliseconds()
            });
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // HU-18 | POST /Estilista/FinalizarAtencion
        // Registra la hora de finalización, devuelve el resumen y deja el estado
        // en "Completada" SOLO después de que el estilista confirme el resumen
        // en el cliente (acción separada ConfirmarFinalizacion).
        //
        // Esta acción solo genera el resumen; ConfirmarFinalizacion persiste.
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarAtencion(int id)
        {
            var check = EstilistaOnly();
            if (check != null)
                return Json(new { success = false, message = "No autorizado" });
 
            var cita = await _db.Citas.FindAsync(id);
 
            if (cita == null)
                return Json(new { success = false, message = "Cita no encontrada" });
 
            // HU-18 tarea T2: solo el estilista asignado
            if (cita.EstilistaId != EstilistaId)
                return Json(new { success = false, message = "No tienes permiso para finalizar esta cita" });
 
            // HU-18 tarea T1: la cita debe estar En proceso
            if (cita.Estado != EstadoCita.EnProceso)
                return Json(new { success = false, message = "Solo puedes finalizar citas que estén en proceso" });
 
            var horaFin = DateTime.Now;
            var horaInicio = cita.HoraInicioAtencion ?? horaFin.AddMinutes(-1);
            var duracion = horaFin - horaInicio;
 
            return Json(new
            {
                success          = true,
                horaInicio       = horaInicio.ToString("HH:mm:ss"),
                horaFin          = horaFin.ToString("HH:mm:ss"),
                horaFinTick      = new DateTimeOffset(horaFin).ToUnixTimeMilliseconds(),
                duracionMinutos  = (int)duracion.TotalMinutes,
                duracionTexto    = $"{(int)duracion.TotalHours:D2}:{duracion.Minutes:D2}:{duracion.Seconds:D2}"
            });
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // HU-18 | POST /Estilista/ConfirmarFinalizacion
        // El estilista vio el resumen y presionó "Aceptar".
        // Persiste la hora de fin y cambia el estado a Completada.
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarFinalizacion(int id, long horaFinTick)
        {
            var check = EstilistaOnly();
            if (check != null)
                return Json(new { success = false, message = "No autorizado" });
 
            var cita = await _db.Citas.FindAsync(id);
 
            if (cita == null)
                return Json(new { success = false, message = "Cita no encontrada" });
 
            if (cita.EstilistaId != EstilistaId)
                return Json(new { success = false, message = "No autorizado" });
 
            if (cita.Estado != EstadoCita.EnProceso)
                return Json(new { success = false, message = "Estado inválido para finalizar" });
 
            // HU-18 tarea T3: registrar hora exacta de finalización
            cita.HoraFinAtencion = DateTimeOffset.FromUnixTimeMilliseconds(horaFinTick).LocalDateTime;
 
            // HU-18 tarea T6: actualizar estado a Completada
            cita.Estado = EstadoCita.Completada;
 
            await _db.SaveChangesAsync();
 
            return Json(new { success = true });
        }
 
        // ──────────────────────────────────────────────────────────────────────
        // Acción heredada — mantenida por compatibilidad con código existente.
        // Las nuevas HU usan IniciarAtencion / FinalizarAtencion.
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoCita nuevoEstado)
        {
            var check = EstilistaOnly();
            if (check != null)
                return Json(new { success = false, message = "No autorizado" });
 
            var cita = await _db.Citas.FindAsync(id);
            if (cita == null)
                return Json(new { success = false, message = "Cita no encontrada" });
 
            if (cita.EstilistaId != EstilistaId)
                return Json(new { success = false, message = "No autorizado" });
 
            if (nuevoEstado == EstadoCita.Cancelada)
                return Json(new { success = false, message = "El estilista no puede cancelar citas" });
 
            cita.Estado = nuevoEstado;
            await _db.SaveChangesAsync();
 
            return Json(new { success = true, message = "Estado actualizado", estado = nuevoEstado.ToString() });
        }
    }
}