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

        private IActionResult AdminOnly()
        {
            if (HttpContext.Session.GetInt32("ClienteId") == null)
                return RedirectToAction("Login", "Account");
            if (!IsAdmin)
                return RedirectToAction("Index", "Home");
            return null!;
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

        // GET /Admin/Servicios - Mostrar servicios agrupados por categoría
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
    return View(servicios);  // ✅ Esto está bien: retorna servicios
}
        

        // GET /Admin/CrearServicio - Mostrar formulario
        public async Task<IActionResult> CrearServicio()
        {
            var check = AdminOnly(); if (check != null) return check;

            ViewBag.Categorias = await _db.CategoriasServicios
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(new Servicio());
        }

        // POST /Admin/CrearServicio
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearServicio(Servicio model, IFormFile? Imagen)
        {
            var check = AdminOnly(); if (check != null) return check;

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _db.CategoriasServicios
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
                TempData["Error"] = "Datos inválidos.";
                return View(model);
            }

            // Verificar que la categoría existe
            if (model.CategoriaServicioId.HasValue)
            {
                var categoriaExiste = await _db.CategoriasServicios
                    .AnyAsync(c => c.Id == model.CategoriaServicioId.Value && c.Activo);
                
                if (!categoriaExiste)
                {
                    ViewBag.Categorias = await _db.CategoriasServicios
                        .Where(c => c.Activo)
                        .OrderBy(c => c.Nombre)
                        .ToListAsync();
                    TempData["Error"] = "La categoría seleccionada no existe.";
                    return View(model);
                }
            }

            // Manejar la imagen
            if (Imagen != null && Imagen.Length > 0)
            {
                if (Imagen.Length > 2 * 1024 * 1024) // 2MB límite
                {
                    TempData["Error"] = "La imagen no puede superar los 2MB.";
                    return RedirectToAction("Servicios");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await Imagen.CopyToAsync(memoryStream);
                    model.ImagenBytes = memoryStream.ToArray();
                    model.ImagenContentType = Imagen.ContentType;
                }
            }

            model.Activo = true;
            _db.Servicios.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Servicio '{model.Nombre}' creado correctamente.";
            return RedirectToAction("Servicios");
        }

        // GET /Admin/EditarServicio/5
        public async Task<IActionResult> EditarServicio(int id)
        {
            var check = AdminOnly(); if (check != null) return check;

            var servicio = await _db.Servicios.FindAsync(id);
            if (servicio == null)
            {
                TempData["Error"] = "Servicio no encontrado.";
                return RedirectToAction("Servicios");
            }

            ViewBag.Categorias = await _db.CategoriasServicios
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(servicio);
        }

        // POST /Admin/EditarServicio
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarServicio(Servicio model, IFormFile? Imagen, bool EliminarImagen = false)
        {
            var check = AdminOnly(); if (check != null) return check;

            var svc = await _db.Servicios.FindAsync(model.Id);
            if (svc == null)
            {
                TempData["Error"] = "Servicio no encontrado.";
                return RedirectToAction("Servicios");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _db.CategoriasServicios
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
                return View(model);
            }

            // Verificar que la categoría existe
            if (model.CategoriaServicioId.HasValue)
            {
                var categoriaExiste = await _db.CategoriasServicios
                    .AnyAsync(c => c.Id == model.CategoriaServicioId.Value && c.Activo);
                
                if (!categoriaExiste)
                {
                    ViewBag.Categorias = await _db.CategoriasServicios
                        .Where(c => c.Activo)
                        .OrderBy(c => c.Nombre)
                        .ToListAsync();
                    TempData["Error"] = "La categoría seleccionada no existe.";
                    return View(model);
                }
            }

            // Actualizar campos
            svc.Nombre = model.Nombre;
            svc.Descripcion = model.Descripcion;
            svc.DuracionHoras = model.DuracionHoras;
            svc.Precio = model.Precio;
            svc.Activo = model.Activo;
            svc.CategoriaServicioId = model.CategoriaServicioId;

            // Manejar imagen
            if (EliminarImagen)
            {
                svc.ImagenBytes = null;
                svc.ImagenContentType = null;
            }
            else if (Imagen != null && Imagen.Length > 0)
            {
                if (Imagen.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "La imagen no puede superar los 2MB.";
                    return RedirectToAction("Servicios");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await Imagen.CopyToAsync(memoryStream);
                    svc.ImagenBytes = memoryStream.ToArray();
                    svc.ImagenContentType = Imagen.ContentType;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Servicio '{svc.Nombre}' actualizado correctamente.";
            return RedirectToAction("Servicios");
        }

        // POST /Admin/EliminarServicio/5 (Soft delete - solo desactiva)
        [HttpPost]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var check = AdminOnly(); if (check != null) return Json(new { success = false, message = "No autorizado" });

            var svc = await _db.Servicios.FindAsync(id);
            if (svc == null)
            {
                return Json(new { success = false, message = "Servicio no encontrado" });
            }

            // Verificar si tiene citas asociadas
            var tieneCitas = await _db.Citas.AnyAsync(c => c.ServicioId == id && c.Estado != EstadoCita.Cancelada);
            if (tieneCitas)
            {
                return Json(new { success = false, message = "No se puede eliminar el servicio porque tiene citas asociadas" });
            }

            svc.Activo = false;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Servicio desactivado correctamente" });
        }
        [HttpGet]
[HttpGet]
public async Task<IActionResult> ObtenerServicio(int id)
{
    var check = AdminOnly(); 
    if (check != null) return Json(new { success = false, message = "No autorizado" });

    var servicio = await _db.Servicios.FindAsync(id);
    if (servicio == null)
    {
        return Json(new { success = false, message = "Servicio no encontrado" });
    }

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
            servicio.Activo,
            servicio.CategoriaServicioId
        }
    });
}

        // GET /Admin/Estilistas
        public async Task<IActionResult> Estilistas()
        {
            var check = AdminOnly(); if (check != null) return check;
            return View(await _db.Estilistas.ToListAsync());
        }

        // POST /Admin/CrearEstilista
        // GET /Admin/ObtenerEstilista/5
// GET /Admin/ObtenerEstilista/5
[HttpGet]
public async Task<IActionResult> ObtenerEstilista(int id)
{
    var check = AdminOnly(); 
    if (check != null) return Json(new { success = false, message = "No autorizado" });

    var estilista = await _db.Estilistas.FindAsync(id);
    if (estilista == null)
    {
        return Json(new { success = false, message = "Estilista no encontrado" });
    }

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

// POST /Admin/CrearEstilista
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> CrearEstilista(Estilista model, IFormFile? Foto)
{
    var check = AdminOnly(); if (check != null) return check;

    // Manejar la foto
    if (Foto != null && Foto.Length > 0)
    {
        if (Foto.Length > 2 * 1024 * 1024)
        {
            TempData["Error"] = "La foto no puede superar los 2MB.";
            return RedirectToAction("Estilistas");
        }

        using (var memoryStream = new MemoryStream())
        {
            await Foto.CopyToAsync(memoryStream);
            model.FotoBytes = memoryStream.ToArray();
            model.FotoContentType = Foto.ContentType;
        }
    }

    model.Activo = true;
    _db.Estilistas.Add(model);
    await _db.SaveChangesAsync();
    TempData["Success"] = $"Estilista '{model.Nombre}' creado correctamente.";
    return RedirectToAction("Estilistas");
}

// POST /Admin/EditarEstilista
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> EditarEstilista(Estilista model, IFormFile? Foto, bool EliminarFoto = false)
{
    var check = AdminOnly(); if (check != null) return check;

    var est = await _db.Estilistas.FindAsync(model.Id);
    if (est == null) return NotFound();

    est.Nombre = model.Nombre;
    est.Especialidad = model.Especialidad;
    est.HoraInicioTrabajo = model.HoraInicioTrabajo;
    est.HoraFinTrabajo = model.HoraFinTrabajo;
    est.HoraInicioDescanso = model.HoraInicioDescanso;
    est.HoraFinDescanso = model.HoraFinDescanso;
    est.Activo = model.Activo;

    // Manejar foto
    if (EliminarFoto)
    {
        est.FotoBytes = null;
        est.FotoContentType = null;
    }
    else if (Foto != null && Foto.Length > 0)
    {
        if (Foto.Length > 2 * 1024 * 1024)
        {
            TempData["Error"] = "La foto no puede superar los 2MB.";
            return RedirectToAction("Estilistas");
        }

        using (var memoryStream = new MemoryStream())
        {
            await Foto.CopyToAsync(memoryStream);
            est.FotoBytes = memoryStream.ToArray();
            est.FotoContentType = Foto.ContentType;
        }
    }

    await _db.SaveChangesAsync();
    TempData["Success"] = $"Estilista '{est.Nombre}' actualizado correctamente.";
    return RedirectToAction("Estilistas");
}

// POST /Admin/EliminarEstilista/5
[HttpPost]
public async Task<IActionResult> EliminarEstilista(int id)
{
    var check = AdminOnly(); if (check != null) return Json(new { success = false, message = "No autorizado" });

    var est = await _db.Estilistas.FindAsync(id);
    if (est == null) return Json(new { success = false, message = "Estilista no encontrado" });

    // Verificar si tiene citas asociadas
    var tieneCitas = await _db.Citas.AnyAsync(c => c.EstilistaId == id);
    if (tieneCitas)
    {
        return Json(new { success = false, message = "No se puede eliminar porque tiene citas asociadas" });
    }

    _db.Estilistas.Remove(est);
    await _db.SaveChangesAsync();
    return Json(new { success = true });
}

        // GET /Admin/Citas
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
    }
}