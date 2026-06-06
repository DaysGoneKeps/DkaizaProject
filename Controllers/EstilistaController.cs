using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Data;
using DkaizaProject.Models;

namespace DkaizaProject.Controllers
{
    public class EstilistaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public EstilistaController(ApplicationDbContext db) => _db = db;

        private bool EsEstilista => HttpContext.Session.GetString("EsEstilista") == "True";
        private int? EstilistaId => HttpContext.Session.GetInt32("EstilistaId");

        private IActionResult? EstilistaOnly()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!EsEstilista || EstilistaId == null)
                return RedirectToAction("Index", "Home");
            return null;
        }

        // GET /Estilista
        public async Task<IActionResult> Index()
        {
            var check = EstilistaOnly(); if (check != null) return check;

            var citas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.EstilistaId == EstilistaId
                    && c.Estado != EstadoCita.Cancelada
                    && c.Estado != EstadoCita.Pagada)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .ToListAsync();

            return View(citas);
        }

        // GET /Estilista/DetalleCita/5
        public async Task<IActionResult> DetalleCita(int id)
        {
            var check = EstilistaOnly(); if (check != null) return check;

            var cita = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Include(c => c.Pago)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cita == null) return NotFound();
            if (cita.EstilistaId != EstilistaId) return Forbid();

            return View(cita);
        }

        // POST /Estilista/CambiarEstado
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoCita nuevoEstado)
        {
            var check = EstilistaOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            var cita = await _db.Citas.FindAsync(id);
            if (cita == null) return Json(new { success = false, message = "Cita no encontrada" });
            if (cita.EstilistaId != EstilistaId) return Json(new { success = false, message = "No autorizado" });

            if (nuevoEstado == EstadoCita.Cancelada)
                return Json(new { success = false, message = "El estilista no puede cancelar citas" });

            cita.Estado = nuevoEstado;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Estado actualizado", estado = nuevoEstado.ToString() });
        }
    }
}
