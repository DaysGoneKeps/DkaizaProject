using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Data;
using DkaizaProject.Models;

namespace DkaizaProject.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA DE CLIENTES
        public IActionResult Index()
        {
            var clientes = _context.Clientes
                .Where(c =>
                    !c.EsAdmin &&
                    !c.EsRecepcionista &&
                    !c.EsEstilista)
                .ToList();

            return View(clientes);
        }

        // FORMULARIO
        public IActionResult Create()
        {
            return View();
        }

        // GUARDAR CLIENTE
       // GUARDAR CLIENTE
[HttpPost]
public IActionResult Create(Cliente cliente)
{
    // VALIDAR CORREO DUPLICADO
    var existeCorreo = _context.Clientes
        .Any(c => c.Email == cliente.Email);

    if (existeCorreo)
    {
        ModelState.AddModelError("",
            "El correo electrónico ya se encuentra vinculado a otra cuenta");
    }

    // QUITAR VALIDACION DE PASSWORD
    ModelState.Remove("PasswordHash");

    // PASSWORD TEMPORAL
    cliente.PasswordHash = "cliente123";

    if (ModelState.IsValid)
    {
        _context.Clientes.Add(cliente);

        _context.SaveChanges();

        TempData["Mensaje"] = "CLIENTE REGISTRADO OK";

        return RedirectToAction("Create");
    }

    return View(cliente);
}

        // GET /Clientes/RegistrarServicio/5
        public async Task<IActionResult> RegistrarServicio(int id)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
            if (cliente == null)
            {
                TempData["Mensaje"] = "Cliente no encontrado";
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        // GET /Clientes/ApiServicios
        [HttpGet]
        public async Task<IActionResult> ApiServicios()
        {
            var servicios = await _context.Servicios
                .Where(s => s.Activo)
                .Select(s => new { id = s.Id, nombre = s.Nombre, precio = s.Precio, duracion = s.DuracionHoras })
                .ToListAsync();
            return Json(servicios);
        }

        // GET /Clientes/ApiEstilistas
        [HttpGet]
        public async Task<IActionResult> ApiEstilistas()
        {
            var estilistas = await _context.Estilistas
                .Where(e => e.Activo)
                .Select(e => new { id = e.Id, nombre = e.Nombre, especialidad = e.Especialidad ?? "" })
                .ToListAsync();
            return Json(estilistas);
        }

        // GET /Clientes/ApiHorarios
        [HttpGet]
        public async Task<IActionResult> ApiHorarios(int servicioId, int estilistaId, string fecha)
        {
            if (!DateTime.TryParse(fecha, out var fechaDate))
                return Json(new { success = false, message = "Fecha inválida" });

            var servicio = await _context.Servicios.FindAsync(servicioId);
            var estilista = await _context.Estilistas.FindAsync(estilistaId);
            if (servicio == null || estilista == null)
                return Json(new { success = false, message = "Datos inválidos" });

            var citas = await _context.Citas
                .Where(c => c.Fecha.Date == fechaDate.Date && c.EstilistaId == estilistaId && c.Estado != EstadoCita.Cancelada)
                .ToListAsync();

            var slots = new List<object>();
            for (int h = estilista.HoraInicioTrabajo; h + servicio.DuracionHoras <= estilista.HoraFinTrabajo; h++)
            {
                int fin = h + servicio.DuracionHoras;
                bool enDescanso = h < estilista.HoraFinDescanso && fin > estilista.HoraInicioDescanso;
                bool enCita = citas.Any(c => c.HoraInicio < fin && c.HoraFin > h);
                bool disponible = !enDescanso && !enCita;
                slots.Add(new { horaInicio = h, horaFin = fin, disponible, label = $"{h:D2}:00 - {fin:D2}:00" });
            }

            return Json(new { success = true, slots });
        }

        // POST /Clientes/RegistrarServicio
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarServicio(
            int id, int servicioId, int estilistaId, string fecha, int horaInicio, decimal monto, string metodo)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
            if (cliente == null) return Json(new { success = false, message = "Cliente no encontrado" });

            if (!DateTime.TryParse(fecha, out var fechaDate))
                return Json(new { success = false, message = "Fecha inválida" });
            if (monto <= 0) return Json(new { success = false, message = "El monto debe ser mayor a cero" });
            if (string.IsNullOrWhiteSpace(metodo)) return Json(new { success = false, message = "Indique el método de pago" });

            var servicio = await _context.Servicios.FindAsync(servicioId);
            var estilista = await _context.Estilistas.FindAsync(estilistaId);
            if (servicio == null || estilista == null)
                return Json(new { success = false, message = "Servicio o estilista inválido" });

            int horaFin = horaInicio + servicio.DuracionHoras;
            if (horaInicio < estilista.HoraInicioTrabajo || horaFin > estilista.HoraFinTrabajo)
                return Json(new { success = false, message = "Horario fuera del turno del estilista" });
            if (horaInicio < estilista.HoraFinDescanso && horaFin > estilista.HoraInicioDescanso)
                return Json(new { success = false, message = "El horario coincide con el descanso del estilista" });

            var conflicto = await _context.Citas.AnyAsync(c =>
                c.Fecha.Date == fechaDate.Date &&
                c.EstilistaId == estilistaId &&
                c.Estado != EstadoCita.Cancelada &&
                c.HoraInicio < horaFin &&
                c.HoraFin > horaInicio);
            if (conflicto)
                return Json(new { success = false, message = "El horario ya está reservado" });

            var ahora = DateTime.Now;
            var cita = new Cita
            {
                ClienteId = cliente.Id,
                ServicioId = servicioId,
                EstilistaId = estilistaId,
                Fecha = fechaDate.Date,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                Estado = EstadoCita.Pendiente,
                FechaCreacion = ahora
            };
            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            var pago = new Pago
            {
                CitaId = cita.Id,
                ExternalReference = Guid.NewGuid().ToString("N"),
                Monto = monto,
                MontoTotal = monto,
                Metodo = metodo,
                Estado = EstadoPago.Pendiente,
                FechaCreacion = ahora,
                Validado = false
            };
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cita y pago registrados", citaId = cita.Id, pagoId = pago.Id });
        }
    }
}