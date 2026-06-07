using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Models;
using System.Text.Json;
using DkaizaProject.Data;

namespace DkaizaProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
 
        public AdminController(ApplicationDbContext db) => _db = db;
 
        private bool IsAdmin => HttpContext.Session.GetString("EsAdmin") == "True";
 
        private IActionResult? AdminOnly()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!IsAdmin)
                return RedirectToAction("Index", "Home");
            return null;
        }
 
        // GET /Admin
        public async Task<IActionResult> Index()
        {
            var check = AdminOnly(); if (check != null) return check;
 
            ViewBag.TotalCitas = await _db.Citas.CountAsync(c => c.Estado != EstadoCita.Cancelada);
            ViewBag.CitasHoy = await _db.Citas.CountAsync(c => c.Fecha.Date == DateTime.Today && c.Estado != EstadoCita.Cancelada);
            ViewBag.TotalClientes = await _db.Clientes.CountAsync(c => !c.EsAdmin);
            ViewBag.TotalServicios = await _db.Servicios.CountAsync(s => s.Activo);
 
            var proximasCitas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.Fecha.Date >= DateTime.Today && c.Estado != EstadoCita.Cancelada)
                .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
                .Take(10)
                .ToListAsync();
 
            return View(proximasCitas);
        }
 
        // ============================================================
        // SERVICIOS
        // ============================================================
 
        public async Task<IActionResult> Servicios()
        {
            var check = AdminOnly(); if (check != null) return check;
 
            var servicios = await _db.Servicios
                .Include(s => s.Categoria)
                .OrderBy(s => s.Categoria != null ? s.Categoria.Orden : 999)
                .ThenBy(s => s.Nombre)
                .ToListAsync();
 
            var categorias = await _db.CategoriasServicios
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();
 
            ViewBag.Categorias = categorias;
            return View(servicios);
        }
 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearServicio([FromForm] Servicio model, IFormFile? Imagen)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            try
            {
                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return Json(new { success = false, message = "El nombre del servicio es requerido" });
 
                if (model.DuracionHoras < 1 || model.DuracionHoras > 8)
                    return Json(new { success = false, message = "La duración debe ser entre 1 y 8 horas" });
 
                if (model.Precio <= 0)
                    return Json(new { success = false, message = "El precio debe ser mayor a 0" });
 
                if (model.CategoriaServicioId.HasValue && model.CategoriaServicioId.Value > 0)
                {
                    var categoriaExiste = await _db.CategoriasServicios
                        .AnyAsync(c => c.Id == model.CategoriaServicioId.Value && c.Activo);
                    if (!categoriaExiste)
                        return Json(new { success = false, message = "La categoría seleccionada no existe" });
                }
 
                if (Imagen != null && Imagen.Length > 0)
                {
                    if (Imagen.Length > 2 * 1024 * 1024)
                        return Json(new { success = false, message = "La imagen no puede superar los 2MB" });
 
                    using var memoryStream = new MemoryStream();
                    await Imagen.CopyToAsync(memoryStream);
                    model.ImagenBytes = memoryStream.ToArray();
                    model.ImagenContentType = Imagen.ContentType;
                }
 
                model.Activo = true;
                _db.Servicios.Add(model);
                await _db.SaveChangesAsync();
 
                return Json(new { success = true, message = $"Servicio '{model.Nombre}' creado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear servicio: " + ex.Message });
            }
        }
 
        [HttpGet]
        public async Task<IActionResult> ObtenerServicio(int id)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var servicio = await _db.Servicios.FindAsync(id);
            if (servicio == null)
                return Json(new { success = false, message = "Servicio no encontrado" });
 
            return Json(new
            {
                success = true,
                servicio = new
                {
                    servicio.Id,
                    servicio.Nombre,
                    servicio.Descripcion,
                    servicio.DuracionHoras,
                    servicio.Precio,
                    Activo = servicio.Activo,
                    servicio.CategoriaServicioId,
                    TieneImagen = servicio.ImagenBytes != null
                }
            });
        }
 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarServicio(
            int Id,
            string Nombre,
            int? CategoriaServicioId,
            string? Descripcion,
            int DuracionHoras,
            decimal Precio,
            bool Activo,
            IFormFile? Imagen,
            bool EliminarImagen = false)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            try
            {
                var servicioExistente = await _db.Servicios.FindAsync(Id);
                if (servicioExistente == null)
                    return Json(new { success = false, message = "Servicio no encontrado" });
 
                if (string.IsNullOrWhiteSpace(Nombre))
                    return Json(new { success = false, message = "El nombre del servicio es requerido" });
 
                if (DuracionHoras < 1 || DuracionHoras > 8)
                    return Json(new { success = false, message = "La duración debe ser entre 1 y 8 horas" });
 
                if (Precio <= 0)
                    return Json(new { success = false, message = "El precio debe ser mayor a 0" });
 
                if (CategoriaServicioId.HasValue && CategoriaServicioId.Value > 0)
                {
                    var categoriaExiste = await _db.CategoriasServicios
                        .AnyAsync(c => c.Id == CategoriaServicioId.Value && c.Activo);
                    if (!categoriaExiste)
                        return Json(new { success = false, message = "La categoría seleccionada no existe" });
                }
 
                servicioExistente.Nombre = Nombre;
                servicioExistente.CategoriaServicioId = CategoriaServicioId;
                servicioExistente.Descripcion = Descripcion;
                servicioExistente.DuracionHoras = DuracionHoras;
                servicioExistente.Precio = Precio;
                servicioExistente.Activo = Activo;
 
                if (EliminarImagen)
                {
                    servicioExistente.ImagenBytes = null;
                    servicioExistente.ImagenContentType = null;
                }
                else if (Imagen != null && Imagen.Length > 0)
                {
                    if (Imagen.Length > 2 * 1024 * 1024)
                        return Json(new { success = false, message = "La imagen no puede superar los 2MB" });
 
                    using var memoryStream = new MemoryStream();
                    await Imagen.CopyToAsync(memoryStream);
                    servicioExistente.ImagenBytes = memoryStream.ToArray();
                    servicioExistente.ImagenContentType = Imagen.ContentType;
                }
 
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"Servicio '{servicioExistente.Nombre}' actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar servicio: " + ex.Message });
            }
        }
 
        [HttpPost]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var servicio = await _db.Servicios.FindAsync(id);
            if (servicio == null)
                return Json(new { success = false, message = "Servicio no encontrado" });
 
            var tieneCitas = await _db.Citas.AnyAsync(c => c.ServicioId == id && c.Estado != EstadoCita.Cancelada);
            if (tieneCitas)
                return Json(new { success = false, message = "No se puede eliminar el servicio porque tiene citas asociadas" });
 
            _db.Servicios.Remove(servicio);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Servicio eliminado correctamente" });
        }


        // GET /Admin/ReporteServicios?anio=2025
public async Task<IActionResult> ReporteServicios(int? anio)
{
    var check = AdminOnly(); if (check != null) return check;
 
    // Si no se pasa año, usar el actual
    int anioSeleccionado = anio ?? DateTime.Today.Year;
 
    // Rango del año completo
    var inicio = new DateTime(anioSeleccionado, 1, 1);
    var fin    = new DateTime(anioSeleccionado, 12, 31, 23, 59, 59);
 
    // Solo citas completadas o pagadas (atendidas)
    var citasAnio = await _db.Citas
        .Include(c => c.Servicio)
        .Include(c => c.Estilista)
        .Where(c =>
            c.Fecha >= inicio &&
            c.Fecha <= fin &&
            (c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Pagada))
        .ToListAsync();
 
    // Años disponibles para el selector (desde el primer registro hasta hoy)
    var anioMinimo = await _db.Citas
        .Where(c => c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Pagada)
        .OrderBy(c => c.Fecha)
        .Select(c => (int?)c.Fecha.Year)
        .FirstOrDefaultAsync() ?? DateTime.Today.Year;
 
    var aniosDisponibles = Enumerable
        .Range(anioMinimo, DateTime.Today.Year - anioMinimo + 1)
        .OrderByDescending(y => y)
        .ToList();
 
    // ── Resumen mensual ──────────────────────────────────────
    var meses = Enumerable.Range(1, 12).Select(mes =>
    {
        var citasMes = citasAnio.Where(c => c.Fecha.Month == mes).ToList();
        return new ResumenMensual
        {
            Mes            = mes,
            NombreMes      = new DateTime(anioSeleccionado, mes, 1).ToString("MMMM"),
            TotalAtendidas = citasMes.Count,
            ServicioTop    = citasMes
                .GroupBy(c => c.Servicio?.Nombre ?? "—")
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—",
            EstilistaTop   = citasMes
                .GroupBy(c => c.Estilista?.Nombre ?? "—")
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—"
        };
    }).ToList();
 
    // ── KPIs anuales ─────────────────────────────────────────
    ViewBag.AnioSeleccionado  = anioSeleccionado;
    ViewBag.AniosDisponibles  = aniosDisponibles;
    ViewBag.TotalAnual        = citasAnio.Count;
    ViewBag.PromedioMensual   = citasAnio.Count > 0
        ? Math.Round(citasAnio.Count / 12.0, 1) : 0.0;
    ViewBag.MesPico           = meses.OrderByDescending(m => m.TotalAtendidas)
                                     .FirstOrDefault()?.NombreMes ?? "—";
    ViewBag.ServicioTop       = citasAnio
        .GroupBy(c => c.Servicio?.Nombre ?? "—")
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? "—";
    ViewBag.EstilistaTop      = citasAnio
        .GroupBy(c => c.Estilista?.Nombre ?? "—")
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? "—";
 
    return View(meses);
}
 
// GET /Admin/ReporteServiciosPdf?anio=2025
// Genera el reporte en PDF (usa la misma lógica, distinta vista)
public async Task<IActionResult> ReporteServiciosPdf(int? anio)
{
    var check = AdminOnly(); if (check != null) return check;
 
    int anioSeleccionado = anio ?? DateTime.Today.Year;
    var inicio = new DateTime(anioSeleccionado, 1, 1);
    var fin    = new DateTime(anioSeleccionado, 12, 31, 23, 59, 59);
 
    var citasAnio = await _db.Citas
        .Include(c => c.Servicio)
        .Include(c => c.Estilista)
        .Where(c =>
            c.Fecha >= inicio &&
            c.Fecha <= fin &&
            (c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Pagada))
        .ToListAsync();
 
    var meses = Enumerable.Range(1, 12).Select(mes =>
    {
        var citasMes = citasAnio.Where(c => c.Fecha.Month == mes).ToList();
        return new ResumenMensual
        {
            Mes            = mes,
            NombreMes      = new DateTime(anioSeleccionado, mes, 1).ToString("MMMM"),
            TotalAtendidas = citasMes.Count,
            ServicioTop    = citasMes
                .GroupBy(c => c.Servicio?.Nombre ?? "—")
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—",
            EstilistaTop   = citasMes
                .GroupBy(c => c.Estilista?.Nombre ?? "—")
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—"
        };
    }).ToList();
 
    ViewBag.AnioSeleccionado = anioSeleccionado;
    ViewBag.TotalAnual       = citasAnio.Count;
    ViewBag.PromedioMensual  = citasAnio.Count > 0
        ? Math.Round(citasAnio.Count / 12.0, 1) : 0.0;
    ViewBag.MesPico          = meses.OrderByDescending(m => m.TotalAtendidas)
                                    .FirstOrDefault()?.NombreMes ?? "—";
    ViewBag.ServicioTop      = citasAnio
        .GroupBy(c => c.Servicio?.Nombre ?? "—")
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? "—";
    ViewBag.EstilistaTop     = citasAnio
        .GroupBy(c => c.Estilista?.Nombre ?? "—")
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? "—";
 
    // Renderizar como vista de impresión (el JS de la vista llama window.print())
    return View(meses);
}




        
 
        // ============================================================
        // ESTILISTAS
        // ============================================================
 
        public async Task<IActionResult> Estilistas()
        {
            var check = AdminOnly(); if (check != null) return check;
            return View(await _db.Estilistas.ToListAsync());
        }
 
        [HttpGet]
        public async Task<IActionResult> ObtenerEstilista(int id)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var estilista = await _db.Estilistas.FindAsync(id);
            if (estilista == null)
                return Json(new { success = false, message = "Estilista no encontrado" });
 
            return Json(new
            {
                success = true,
                estilista = new
                {
                    estilista.Id,
                    estilista.Nombre,
                    estilista.Especialidad,
                    estilista.HoraInicioTrabajo,
                    estilista.HoraFinTrabajo,
                    estilista.HoraInicioDescanso,
                    estilista.HoraFinDescanso,
                    estilista.Activo
                }
            });
        }
 
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEstilista([FromForm] Estilista model, IFormFile? Foto)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            try
            {
                if (Foto != null && Foto.Length > 0)
                {
                    if (Foto.Length > 2 * 1024 * 1024)
                        return Json(new { success = false, message = "La foto no puede superar los 2MB" });
 
                    using var memoryStream = new MemoryStream();
                    await Foto.CopyToAsync(memoryStream);
                    model.FotoBytes = memoryStream.ToArray();
                    model.FotoContentType = Foto.ContentType;
                }
 
                model.Activo = true;
                _db.Estilistas.Add(model);
                await _db.SaveChangesAsync();
 
                var (email, password) = await GenerarCredencialesEstilistaAsync(model.Nombre);
                var partes = model.Nombre.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cliente = new Cliente
                {
                    Nombre = partes[0],
                    Apellido = partes.Length > 1 ? partes[1] : "",
                    Email = email,
                    Telefono = "",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    EsAdmin = false,
                    EsEstilista = true,
                    EstilistaId = model.Id,
                    FechaRegistro = DateTime.UtcNow
                };
                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync();
 
                return Json(new
                {
                    success = true,
                    message = $"Estilista '{model.Nombre}' creado correctamente",
                    credenciales = new { email, password }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear estilista: " + ex.Message });
            }
        }
 
        private async Task<(string email, string password)> GenerarCredencialesEstilistaAsync(string nombreCompleto)
        {
            var soloLetras = new string((nombreCompleto ?? "").ToLowerInvariant()
                .Where(c => c >= 'a' && c <= 'z').ToArray());
            if (soloLetras.Length < 4) soloLetras = (soloLetras + "user").Substring(0, 4);
            var baseNombre = soloLetras.Substring(0, 4);
 
            var rng = new Random();
            string email;
            int intentos = 0;
            do
            {
                email = $"{baseNombre}{rng.Next(100, 1000)}@dkaiza.com";
                intentos++;
            } while (await _db.Clientes.AnyAsync(c => c.Email == email) && intentos < 20);
 
            var prefijoPwd = (baseNombre.Length >= 3 ? baseNombre.Substring(0, 3) : baseNombre + "x");
            prefijoPwd = char.ToUpperInvariant(prefijoPwd[0]) + prefijoPwd.Substring(1);
            var password = $"{prefijoPwd}{rng.Next(1000, 10000)}";
 
            return (email, password);
        }
 
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEstilista([FromForm] Estilista model, IFormFile? Foto, bool EliminarFoto = false)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            try
            {
                var est = await _db.Estilistas.FindAsync(model.Id);
                if (est == null)
                    return Json(new { success = false, message = "Estilista no encontrado" });
 
                est.Nombre = model.Nombre;
                est.Especialidad = model.Especialidad;
                est.HoraInicioTrabajo = model.HoraInicioTrabajo;
                est.HoraFinTrabajo = model.HoraFinTrabajo;
                est.HoraInicioDescanso = model.HoraInicioDescanso;
                est.HoraFinDescanso = model.HoraFinDescanso;
                est.Activo = model.Activo;
 
                if (EliminarFoto)
                {
                    est.FotoBytes = null;
                    est.FotoContentType = null;
                }
                else if (Foto != null && Foto.Length > 0)
                {
                    if (Foto.Length > 2 * 1024 * 1024)
                        return Json(new { success = false, message = "La foto no puede superar los 2MB" });
 
                    using var memoryStream = new MemoryStream();
                    await Foto.CopyToAsync(memoryStream);
                    est.FotoBytes = memoryStream.ToArray();
                    est.FotoContentType = Foto.ContentType;
                }
 
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"Estilista '{est.Nombre}' actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar estilista: " + ex.Message });
            }
        }
 
        [HttpPost]
        public async Task<IActionResult> EliminarEstilista(int id)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var est = await _db.Estilistas.FindAsync(id);
            if (est == null) return Json(new { success = false, message = "Estilista no encontrado" });
 
            var tieneCitas = await _db.Citas.AnyAsync(c => c.EstilistaId == id && c.Estado != EstadoCita.Cancelada);
            if (tieneCitas)
                return Json(new { success = false, message = "No se puede eliminar porque tiene citas asociadas" });
 
            _db.Estilistas.Remove(est);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Estilista eliminado correctamente" });
        }
 
        // ============================================================
        // CITAS
        // ============================================================
 
        public async Task<IActionResult> Citas()
        {
            var check = AdminOnly(); if (check != null) return check;
            var citas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
            return View(citas);
        }
 
        public async Task<IActionResult> AgendaDiaria()
        {
            var check = AdminOnly(); if (check != null) return check;
 
            var hoy = DateTime.Today;
 
            var estilistas = await _db.Estilistas
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .ToListAsync();
 
            var citas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.Fecha.Date == hoy && c.Estado != EstadoCita.Cancelada)
                .OrderBy(c => c.EstilistaId)
                .ThenBy(c => c.HoraInicio)
                .ToListAsync();
 
            ViewBag.FechaSeleccionada = hoy.ToString("yyyy-MM-dd");
            ViewBag.EstilistasAgenda = estilistas;
            return View(citas);
        }
 
        [HttpGet]
        public async Task<IActionResult> AgendaDiariaData(string fecha, int estilistaId = 0)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            if (!DateTime.TryParse(fecha, out var fechaSeleccionada))
                return Json(new { success = false, message = "Fecha inválida" });
 
            var query = _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.Fecha.Date == fechaSeleccionada.Date && c.Estado != EstadoCita.Cancelada);
 
            if (estilistaId > 0)
                query = query.Where(c => c.EstilistaId == estilistaId);
 
            var citas = await query
                .OrderBy(c => c.EstilistaId)
                .ThenBy(c => c.HoraInicio)
                .ToListAsync();
 
            var resultado = citas.Select(c => new
            {
                c.Id,
                Cliente = c.Cliente.Nombre,
                Servicio = c.Servicio.Nombre,
                Estilista = c.Estilista.Nombre,
                EstilistaId = c.EstilistaId,
                HoraInicio = $"{c.HoraInicio:D2}:00",
                HoraFin = $"{c.HoraFin:D2}:00",
                Estado = c.Estado.ToString(),
                Notas = c.Notas ?? ""
            });
 
            return Json(new { success = true, citas = resultado });
        }
 
        // ============================================================
        // INGRESOS
        // ============================================================
 
        public async Task<IActionResult> Ingresos()
        {
            var check = AdminOnly(); if (check != null) return check;
 
            var hoy = DateTime.Today;
 
            var pagos = await _db.Pagos
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Servicio)
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Estilista)
                .Where(p => p.Estado == EstadoPago.Aprobado
                        && p.FechaPago.HasValue
                        && p.FechaPago.Value.Date == hoy)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
 
            ViewBag.FechaDesde = hoy.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = hoy.ToString("yyyy-MM-dd");
            ViewBag.TotalIngresos = pagos.Sum(p => p.MontoTotal);
 
            return View(pagos);
        }
 
        [HttpGet]
        public async Task<IActionResult> IngresosData(string desde, string hasta)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            if (!DateTime.TryParse(desde, out var fechaDesde) ||
                !DateTime.TryParse(hasta, out var fechaHasta))
                return Json(new { success = false, message = "Fechas inválidas" });
 
            fechaDesde = fechaDesde.Date;
            fechaHasta = fechaHasta.Date.AddDays(1).AddTicks(-1);
 
            var pagos = await _db.Pagos
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Servicio)
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Cita)
                    .ThenInclude(c => c.Estilista)
                .Where(p => p.Estado == EstadoPago.Aprobado
                        && p.FechaPago.HasValue
                        && p.FechaPago.Value >= fechaDesde
                        && p.FechaPago.Value <= fechaHasta)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
 
            var resultado = pagos.Select(p => new
            {
                p.Id,
                Servicio  = p.Cita?.Servicio?.Nombre ?? "—",
                Cliente   = p.Cita?.Cliente?.Nombre  ?? "—",
                Estilista = p.Cita?.Estilista?.Nombre ?? "—",
                Monto     = p.MontoTotal,
                Metodo    = p.Metodo ?? "MercadoPago",
                FechaPago = p.FechaPago!.Value.ToString("dd/MM/yyyy"),
                HoraPago  = p.FechaPago!.Value.ToString("HH:mm"),
                PaymentId = p.PaymentId ?? "—"
            });
 
            return Json(new
            {
                success  = true,
                pagos    = resultado,
                total    = pagos.Sum(p => p.MontoTotal),
                cantidad = pagos.Count
            });
        }
 
        // ============================================================
        // HU-10 — HISTORIAL DE CLIENTES
        // ============================================================
 
        // GET /Admin/HistorialClientes
        public async Task<IActionResult> HistorialClientes()
        {
            var check = AdminOnly(); if (check != null) return check;
 
            // Solo clientes reales (excluye admin, estilista, recepcionista)
            var clientes = await _db.Clientes
                .Where(c => !c.EsAdmin && !c.EsEstilista && !c.EsRecepcionista)
                .ToListAsync();
 
            var clienteIds = clientes.Select(c => c.Id).ToList();
 
            var todasCitas = await _db.Citas
                .Where(c => clienteIds.Contains(c.ClienteId))
                .ToListAsync();
 
            var resumen = clientes.Select(c =>
            {
                var citas = todasCitas.Where(x => x.ClienteId == c.Id).ToList();
 
                // Asistió = cita Completada o Pagada
                int asistencias   = citas.Count(x => x.Estado == EstadoCita.Completada || x.Estado == EstadoCita.Pagada);
                // Canceló = cita en estado Cancelada
                int cancelaciones = citas.Count(x => x.Estado == EstadoCita.Cancelada);
                // No asistió = fecha pasada, no cancelada, no completada/pagada
                int ausencias     = citas.Count(x =>
                    x.Fecha.Date < DateTime.Today &&
                    x.Estado != EstadoCita.Cancelada &&
                    x.Estado != EstadoCita.Completada &&
                    x.Estado != EstadoCita.Pagada);
 
                // Frecuencia basada en asistencias confirmadas:
                // Alta  >= 8  | Media >= 3  | Baja < 3
                string frecuencia = asistencias >= 8 ? "Alta"
                                  : asistencias >= 3 ? "Media"
                                                     : "Baja";
 
                // VIP automático si frecuencia Alta, pero el admin puede
                // también asignarlo/quitarlo manualmente mediante EsVip
                bool esVip = c.EsVip || asistencias >= 8;
 
                return new ClienteHistorialResumen
                {
                    ClienteId      = c.Id,
                    NombreCompleto = c.NombreCompleto,
                    Email          = c.Email,
                    Asistencias    = asistencias,
                    Cancelaciones  = cancelaciones,
                    Ausencias      = ausencias,
                    Frecuencia     = frecuencia,
                    EsVip          = esVip,
                    UltimaVisita   = citas
                        .Where(x => x.Estado == EstadoCita.Completada || x.Estado == EstadoCita.Pagada)
                        .OrderByDescending(x => x.Fecha)
                        .Select(x => (DateTime?)x.Fecha)
                        .FirstOrDefault()
                };
            }).ToList();
 
            ViewBag.TotalClientes     = resumen.Count;
            ViewBag.ClientesVip       = resumen.Count(r => r.EsVip);
            ViewBag.TotalAsistencias  = resumen.Sum(r => r.Asistencias);
            ViewBag.PromedioPorCliente = resumen.Count > 0
                ? Math.Round((double)resumen.Sum(r => r.Asistencias) / resumen.Count, 1)
                : 0.0;
 
            return View(resumen);
        }

        // GET /Admin/PerfilEstilista/5
public async Task<IActionResult> PerfilEstilista(int id, int pagina = 1)
{
    var check = AdminOnly(); if (check != null) return check;

    var estilista = await _db.Estilistas
        .FirstOrDefaultAsync(e => e.Id == id);

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
 
        // GET /Admin/HistorialDetalleCliente/5?desde=&hasta=
        public async Task<IActionResult> HistorialDetalleCliente(int id, string? desde, string? hasta)
        {
            var check = AdminOnly(); if (check != null) return check;
 
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return RedirectToAction("HistorialClientes");
 
            // Validar que sea cliente real
            if (cliente.EsAdmin || cliente.EsEstilista || cliente.EsRecepcionista)
                return RedirectToAction("HistorialClientes");
 
            var query = _db.Citas
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c => c.ClienteId == id);
 
            // Filtro por rango de fechas (criterio de aceptación 4)
            if (DateTime.TryParse(desde, out var dDesde))
                query = query.Where(c => c.Fecha.Date >= dDesde.Date);
            if (DateTime.TryParse(hasta, out var dHasta))
                query = query.Where(c => c.Fecha.Date <= dHasta.Date);
 
            // Ordenado por fecha descendente (regla de negocio 5)
            var citas = await query
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
 
            int asistencias   = citas.Count(x => x.Estado == EstadoCita.Completada || x.Estado == EstadoCita.Pagada);
            int cancelaciones = citas.Count(x => x.Estado == EstadoCita.Cancelada);
            int ausencias     = citas.Count(x =>
                x.Fecha.Date < DateTime.Today &&
                x.Estado != EstadoCita.Cancelada &&
                x.Estado != EstadoCita.Completada &&
                x.Estado != EstadoCita.Pagada);
 
            ViewBag.Cliente        = cliente;
            ViewBag.Asistencias    = asistencias;
            ViewBag.Cancelaciones  = cancelaciones;
            ViewBag.Ausencias      = ausencias;
            ViewBag.TotalRegistros = citas.Count;
            ViewBag.Desde          = desde ?? "";
            ViewBag.Hasta          = hasta  ?? "";
 
            return View(citas);
        }
 
        // POST /Admin/ToggleVip/5
        // Permite al admin asignar o quitar VIP manualmente (regla de negocio 4)
        [HttpPost]
        public async Task<IActionResult> ToggleVip(int id)
        {
            var check = AdminOnly();
            if (check != null) return Json(new { success = false, message = "No autorizado" });
 
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null)
                return Json(new { success = false, message = "Cliente no encontrado" });
 
            // No permitir marcar como VIP a roles internos
            if (cliente.EsAdmin || cliente.EsEstilista || cliente.EsRecepcionista)
                return Json(new { success = false, message = "No se puede modificar este usuario" });
 
            cliente.EsVip = !cliente.EsVip;
            await _db.SaveChangesAsync();
 
            return Json(new { success = true, esVip = cliente.EsVip });
        }
    }
}