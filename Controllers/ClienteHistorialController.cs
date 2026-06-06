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
    public class ClienteHistorialController : Controller
    {
        private readonly ApplicationDbContext _db;
 
        public ClienteHistorialController(ApplicationDbContext db) => _db = db;
 
        // ── Seguridad: solo roles internos (estilista, admin, recepcionista) ──
        private bool TieneAcceso =>
            HttpContext.Session.GetString("EsEstilista")     == "True" ||
            HttpContext.Session.GetString("EsAdmin")         == "True" ||
            HttpContext.Session.GetString("EsRecepcionista") == "True";
 
        private int? EstilistaIdSesion => HttpContext.Session.GetInt32("EstilistaId");
 
        private IActionResult? AccesoInterno()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!TieneAcceso)
                return RedirectToAction("Index", "Home");
            return null;
        }
 
        // ────────────────────────────────────────────────────────────
        // GET /ClienteHistorial
        // Página de búsqueda de clientes
        // ────────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            var check = AccesoInterno(); if (check != null) return check;
            return View();
        }
 
        // ────────────────────────────────────────────────────────────
        // GET /ClienteHistorial/Buscar?q=Maria
        // AJAX: devuelve lista de clientes que coinciden con la búsqueda
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Buscar(string q)
        {
            var check = AccesoInterno();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            if (string.IsNullOrWhiteSpace(q))
                return Json(new { success = false, message = "Ingrese al menos un criterio de búsqueda" });
 
            var termino = q.Trim().ToLower();
 
            var clientes = await _db.Clientes
                .Where(c => !c.EsAdmin && !c.EsEstilista && !c.EsRecepcionista)
                .Where(c =>
                    c.Nombre.ToLower().Contains(termino) ||
                    c.Apellido.ToLower().Contains(termino) ||
                    (c.Nombre + " " + c.Apellido).ToLower().Contains(termino) ||
                    c.Telefono.Contains(termino))
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Apellido,
                    c.Telefono,
                    c.Email,
                    c.FechaRegistro
                })
                .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
                .Take(20)
                .ToListAsync();
 
            return Json(new { success = true, clientes, total = clientes.Count });
        }
 
        // ────────────────────────────────────────────────────────────
        // GET /ClienteHistorial/Detalle/5
        // Vista de detalle: historial + notas + preferencias
        // ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Detalle(int id)
        {
            var check = AccesoInterno(); if (check != null) return check;
 
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null || cliente.EsAdmin || cliente.EsEstilista || cliente.EsRecepcionista)
                return NotFound();
 
            // Historial: citas completadas del cliente
            var historial = await _db.Citas
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.ClienteId == id && c.Estado == EstadoCita.Completada)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
 
            // Notas registradas por estilistas (todas, visibles para todos)
            var notas = await _db.Set<NotaCliente>()
                .Include(n => n.Estilista)
                .Where(n => n.ClienteId == id)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
 
            // Preferencias: servicios más solicitados y finalizados
            var preferencias = historial
                .GroupBy(c => c.Servicio.Nombre)
                .Select(g => new PreferenciaServicio
                {
                    NombreServicio = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(p => p.Cantidad)
                .Take(5)
                .ToList();
 
            // Frecuencia de visita: promedio de días entre citas
            string frecuenciaTexto = "Sin datos";
            if (historial.Count >= 2)
            {
                var fechas = historial.Select(c => c.Fecha).OrderBy(f => f).ToList();
                double promedioDias = 0;
                for (int i = 1; i < fechas.Count; i++)
                    promedioDias += (fechas[i] - fechas[i - 1]).TotalDays;
                promedioDias /= (fechas.Count - 1);
 
                frecuenciaTexto = promedioDias <= 14 ? "Cada 1-2 semanas"
                    : promedioDias <= 30 ? "Cada 3-4 semanas"
                    : promedioDias <= 60 ? "Cada 1-2 meses"
                    : "Cada varios meses";
            }
 
            var vm = new ClienteHistorialViewModel
            {
                Cliente         = cliente,
                Historial       = historial,
                Notas           = notas,
                Preferencias    = preferencias,
                FrecuenciaVisita = frecuenciaTexto
            };
 
            return View(vm);
        }
 
        // ────────────────────────────────────────────────────────────
        // POST /ClienteHistorial/AgregarNota
        // AJAX: estilista agrega una nota de preferencia al cliente
        // ────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarNota(int clienteId, string contenido)
        {
            var check = AccesoInterno();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            if (string.IsNullOrWhiteSpace(contenido))
                return Json(new { success = false, message = "La nota no puede estar vacía" });
 
            if (contenido.Length > 1000)
                return Json(new { success = false, message = "La nota no puede superar los 1000 caracteres" });
 
            // El estilista que está logueado
            var estilistaId = EstilistaIdSesion;
            if (estilistaId == null)
            {
                // Si es admin o recepcionista, usar el primer estilista como fallback
                // En producción se podría pedir seleccionar estilista
                var primerEst = await _db.Estilistas.FirstOrDefaultAsync(e => e.Activo);
                if (primerEst == null)
                    return Json(new { success = false, message = "No hay estilistas disponibles" });
                estilistaId = primerEst.Id;
            }
 
            var nota = new NotaCliente
            {
                ClienteId   = clienteId,
                EstilistaId = estilistaId.Value,
                Contenido   = contenido.Trim(),
                FechaCreacion = DateTime.Now
            };
 
            _db.Set<NotaCliente>().Add(nota);
            await _db.SaveChangesAsync();
 
            // Recargar con nombre del estilista para devolver al frontend
            var estilista = await _db.Estilistas.FindAsync(estilistaId);
 
            return Json(new
            {
                success = true,
                message = "Nota registrada correctamente",
                nota = new
                {
                    nota.Id,
                    nota.Contenido,
                    EstilistaNombre = estilista?.Nombre ?? "Estilista",
                    Fecha = nota.FechaCreacion.ToString("dd MMM yyyy")
                }
            });
        }
 
        // ────────────────────────────────────────────────────────────
        // POST /ClienteHistorial/EliminarNota/5
        // AJAX: elimina una nota (solo el estilista que la creó o admin)
        // ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> EliminarNota(int id)
        {
            var check = AccesoInterno();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var nota = await _db.Set<NotaCliente>().FindAsync(id);
            if (nota == null)
                return Json(new { success = false, message = "Nota no encontrada" });
 
            // Solo el propio estilista o admin puede eliminar
            bool esAdmin = HttpContext.Session.GetString("EsAdmin") == "True";
            bool esPropietario = EstilistaIdSesion.HasValue && nota.EstilistaId == EstilistaIdSesion.Value;
 
            if (!esAdmin && !esPropietario)
                return Json(new { success = false, message = "Solo puedes eliminar tus propias notas" });
 
            _db.Set<NotaCliente>().Remove(nota);
            await _db.SaveChangesAsync();
 
            return Json(new { success = true, message = "Nota eliminada" });
        }
    }
 
    // ── ViewModels internos ──────────────────────────────────────────
    public class ClienteHistorialViewModel
    {
        public Cliente                Cliente          { get; set; } = null!;
        public List<Cita>             Historial        { get; set; } = new();
        public List<NotaCliente>      Notas            { get; set; } = new();
        public List<PreferenciaServicio> Preferencias  { get; set; } = new();
        public string                 FrecuenciaVisita { get; set; } = "";
    }
 
    public class PreferenciaServicio
    {
        public string NombreServicio { get; set; } = string.Empty;
        public int    Cantidad       { get; set; }
    }
}
 