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

        // GET /Admin/Servicios - Mostrar servicios (USA MODALES, no vistas separadas)
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

        // ============================================================
        // IMPORTANTE: NO uses métodos GET para Crear/Editar que devuelvan vistas
        // Todo se maneja con MODALES y AJAX
        // ============================================================

        // ✅ POST: /Admin/CrearServicio - Crea servicio vía AJAX (desde modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearServicio([FromForm] Servicio model, IFormFile? Imagen)
        {
            var check = AdminOnly(); 
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            try
            {
                if (string.IsNullOrWhiteSpace(model.Nombre))
                {
                    return Json(new { success = false, message = "El nombre del servicio es requerido" });
                }

                // Validar duración
                if (model.DuracionHoras < 1 || model.DuracionHoras > 8)
                {
                    return Json(new { success = false, message = "La duración debe ser entre 1 y 8 horas" });
                }

                // Validar precio
                if (model.Precio <= 0)
                {
                    return Json(new { success = false, message = "El precio debe ser mayor a 0" });
                }

                // Verificar que la categoría existe (si se seleccionó)
                if (model.CategoriaServicioId.HasValue && model.CategoriaServicioId.Value > 0)
                {
                    var categoriaExiste = await _db.CategoriasServicios
                        .AnyAsync(c => c.Id == model.CategoriaServicioId.Value && c.Activo);
                    
                    if (!categoriaExiste)
                    {
                        return Json(new { success = false, message = "La categoría seleccionada no existe" });
                    }
                }

                // Procesar imagen
                if (Imagen != null && Imagen.Length > 0)
                {
                    if (Imagen.Length > 2 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "La imagen no puede superar los 2MB" });
                    }

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

        // ✅ GET: /Admin/ObtenerServicio/5 - Para cargar datos en el modal de edición
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
                    Activo = servicio.Activo,
                    servicio.CategoriaServicioId,
                    TieneImagen = servicio.ImagenBytes != null
                }
            });
        }

        // ✅ POST: /Admin/EditarServicio - Actualiza servicio vía AJAX (desde modal)
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditarServicio(
    int Id, 
    string Nombre, 
    int? CategoriaServicioId, 
    string? Descripcion, 
    int DuracionHoras, 
    decimal Precio, 
    bool Activo,  // 🔥 AHORA ES bool, no string
    IFormFile? Imagen, 
    bool EliminarImagen = false)
{
    var check = AdminOnly(); 
    if (check != null) return Json(new { success = false, message = "No autorizado" });

    try
    {
        var servicioExistente = await _db.Servicios.FindAsync(Id);
        if (servicioExistente == null)
        {
            return Json(new { success = false, message = "Servicio no encontrado" });
        }

        // Validaciones
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            return Json(new { success = false, message = "El nombre del servicio es requerido" });
        }

        if (DuracionHoras < 1 || DuracionHoras > 8)
        {
            return Json(new { success = false, message = "La duración debe ser entre 1 y 8 horas" });
        }

        if (Precio <= 0)
        {
            return Json(new { success = false, message = "El precio debe ser mayor a 0" });
        }

        // Verificar que la categoría existe (si se seleccionó)
        if (CategoriaServicioId.HasValue && CategoriaServicioId.Value > 0)
        {
            var categoriaExiste = await _db.CategoriasServicios
                .AnyAsync(c => c.Id == CategoriaServicioId.Value && c.Activo);
            
            if (!categoriaExiste)
            {
                return Json(new { success = false, message = "La categoría seleccionada no existe" });
            }
        }

        // Actualizar datos
        servicioExistente.Nombre = Nombre;
        servicioExistente.CategoriaServicioId = CategoriaServicioId;
        servicioExistente.Descripcion = Descripcion;
        servicioExistente.DuracionHoras = DuracionHoras;
        servicioExistente.Precio = Precio;
        servicioExistente.Activo = Activo;  // 🔥 AHORA ES bool

        // Manejar imagen
        if (EliminarImagen)
        {
            servicioExistente.ImagenBytes = null;
            servicioExistente.ImagenContentType = null;
        }
        else if (Imagen != null && Imagen.Length > 0)
        {
            if (Imagen.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "La imagen no puede superar los 2MB" });
            }

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
        // ✅ POST: /Admin/EliminarServicio/5
        [HttpPost]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var check = AdminOnly(); 
            if (check != null) return Json(new { success = false, message = "No autorizado" });

            var servicio = await _db.Servicios.FindAsync(id);
            if (servicio == null)
            {
                return Json(new { success = false, message = "Servicio no encontrado" });
            }

            // Verificar si tiene citas asociadas
            var tieneCitas = await _db.Citas.AnyAsync(c => c.ServicioId == id && c.Estado != EstadoCita.Cancelada);
            if (tieneCitas)
            {
                return Json(new { success = false, message = "No se puede eliminar el servicio porque tiene citas asociadas" });
            }

            _db.Servicios.Remove(servicio);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Servicio eliminado correctamente" });
        }

        // ============================================================
        // ESTILISTAS
        // ============================================================

        // GET /Admin/Estilistas
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
                    {
                        return Json(new { success = false, message = "La foto no puede superar los 2MB" });
                    }

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
                {
                    return Json(new { success = false, message = "Estilista no encontrado" });
                }

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
                        return Json(new { success = false, message = "La foto no puede superar los 2MB" });
                    }

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

            // Verificar si tiene citas asociadas
            var tieneCitas = await _db.Citas.AnyAsync(c => c.EstilistaId == id && c.Estado != EstadoCita.Cancelada);
            if (tieneCitas)
            {
                return Json(new { success = false, message = "No se puede eliminar porque tiene citas asociadas" });
            }

            _db.Estilistas.Remove(est);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Estilista eliminado correctamente" });
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