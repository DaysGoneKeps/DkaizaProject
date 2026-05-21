using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Data;
using DkaizaProject.Models;

namespace DkaizaProject.Controllers
{
    public class RecepcionistaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RecepcionistaController(ApplicationDbContext db) => _db = db;

        private bool EsRecepcionista => HttpContext.Session.GetString("EsRecepcionista") == "True";

        private IActionResult? RecepcionistaOnly()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!EsRecepcionista)
                return RedirectToAction("Index", "Home");
            return null;
        }

        // GET /Recepcionista
        public async Task<IActionResult> Index()
        {
            var check = RecepcionistaOnly(); if (check != null) return check;

            var hoy = DateTime.Today;
            var saldoCaja = await _db.Pagos
                .Where(p => p.Validado && p.FechaValidacion != null && p.FechaValidacion.Value.Date == hoy)
                .SumAsync(p => (decimal?)p.MontoTotal) ?? 0m;

            var pendientes = await _db.Pagos
                .Include(p => p.Cita).ThenInclude(c => c.Cliente)
                .Include(p => p.Cita).ThenInclude(c => c.Servicio)
                .Where(p => !p.Validado)
                .OrderByDescending(p => p.FechaCreacion)
                .Take(50)
                .ToListAsync();

            ViewBag.SaldoCaja = saldoCaja;
            ViewBag.Pendientes = pendientes;
            return View();
        }

        // POST /Recepcionista/BuscarCita
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarCita(int codigo)
        {
            var check = RecepcionistaOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            var cita = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Include(c => c.Pago)
                .FirstOrDefaultAsync(c => c.Id == codigo);

            if (cita == null) return Json(new { success = false, message = "No se encontró una cita con ese código" });

            return Json(new
            {
                success = true,
                cita = new
                {
                    id = cita.Id,
                    cliente = cita.Cliente.NombreCompleto,
                    telefono = cita.Cliente.Telefono ?? "",
                    servicio = cita.Servicio.Nombre,
                    precio = cita.Servicio.Precio,
                    estilista = cita.Estilista.Nombre,
                    fecha = cita.Fecha.ToString("dd/MM/yyyy"),
                    horario = $"{cita.HoraInicio:D2}:00 - {cita.HoraFin:D2}:00",
                    estadoCita = cita.Estado.ToString(),
                    tienePago = cita.Pago != null,
                    pago = cita.Pago == null ? null : new
                    {
                        id = cita.Pago.Id,
                        monto = cita.Pago.MontoTotal,
                        metodo = cita.Pago.Metodo ?? "",
                        estado = cita.Pago.Estado.ToString(),
                        validado = cita.Pago.Validado,
                        numeroOperacion = cita.Pago.NumeroOperacion
                    }
                }
            });
        }

        // POST /Recepcionista/RegistrarPago
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPago(int citaId, decimal monto, string metodo)
        {
            var check = RecepcionistaOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            var cita = await _db.Citas.Include(c => c.Pago).FirstOrDefaultAsync(c => c.Id == citaId);
            if (cita == null) return Json(new { success = false, message = "Cita no encontrada" });
            if (cita.Pago != null) return Json(new { success = false, message = "La cita ya tiene un pago registrado" });
            if (monto <= 0) return Json(new { success = false, message = "El monto debe ser mayor a cero" });
            if (string.IsNullOrWhiteSpace(metodo)) return Json(new { success = false, message = "Debe indicar el método de pago" });

            var pago = new Pago
            {
                CitaId = cita.Id,
                ExternalReference = Guid.NewGuid().ToString("N"),
                Monto = monto,
                MontoTotal = monto,
                Metodo = metodo,
                Estado = EstadoPago.Pendiente,
                Validado = false,
                FechaCreacion = DateTime.Now
            };
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Pago registrado exitosamente", pagoId = pago.Id });
        }

        // POST /Recepcionista/ValidarPago
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarPago(int pagoId)
        {
            var check = RecepcionistaOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            var pago = await _db.Pagos.Include(p => p.Cita).FirstOrDefaultAsync(p => p.Id == pagoId);
            if (pago == null) return Json(new { success = false, message = "Pago no encontrado" });
            if (pago.Validado) return Json(new { success = false, message = "El pago ya fue validado" });

            var hoy = DateTime.Today;
            var correlativo = await _db.Pagos.CountAsync(p => p.NumeroOperacion != null && p.FechaValidacion != null && p.FechaValidacion.Value.Date == hoy) + 1;
            var numeroOperacion = $"OP-{hoy:yyyyMMdd}-{correlativo:D4}";

            pago.Validado = true;
            pago.Estado = EstadoPago.Aprobado;
            pago.FechaPago = DateTime.Now;
            pago.FechaValidacion = DateTime.Now;
            pago.ValidadoPorClienteId = HttpContext.Session.GetInt32("ClienteId");
            pago.NumeroOperacion = numeroOperacion;
            pago.Cita.Estado = EstadoCita.Pagada;

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Pago validado correctamente", numeroOperacion });
        }
    }
}
