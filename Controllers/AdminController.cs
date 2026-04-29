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

    // GET /Admin/Servicios
    public async Task<IActionResult> Servicios()
    {
        var check = AdminOnly(); if (check != null) return check;
        return View(await _db.Servicios.ToListAsync());
    }

    // POST /Admin/CrearServicio
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearServicio(Servicio model)
    {
        var check = AdminOnly(); if (check != null) return check;
        if (!ModelState.IsValid) { TempData["Error"] = "Datos inválidos."; return RedirectToAction("Servicios"); }
        _db.Servicios.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Servicio creado correctamente.";
        return RedirectToAction("Servicios");
    }

    // POST /Admin/EditarServicio
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarServicio(Servicio model)
    {
        var check = AdminOnly(); if (check != null) return check;
        var svc = await _db.Servicios.FindAsync(model.Id);
        if (svc == null) return NotFound();
        svc.Nombre = model.Nombre;
        svc.Descripcion = model.Descripcion;
        svc.DuracionHoras = model.DuracionHoras;
        svc.Precio = model.Precio;
        svc.Activo = model.Activo;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Servicio actualizado.";
        return RedirectToAction("Servicios");
    }

    // POST /Admin/EliminarServicio/5
    [HttpPost]
    public async Task<IActionResult> EliminarServicio(int id)
    {
        var check = AdminOnly(); if (check != null) return check;
        var svc = await _db.Servicios.FindAsync(id);
        if (svc != null) { svc.Activo = false; await _db.SaveChangesAsync(); }
        return Json(new { success = true });
    }

    // GET /Admin/Estilistas
    public async Task<IActionResult> Estilistas()
    {
        var check = AdminOnly(); if (check != null) return check;
        return View(await _db.Estilistas.ToListAsync());
    }

    // POST /Admin/CrearEstilista
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEstilista(Estilista model)
    {
        var check = AdminOnly(); if (check != null) return check;
        _db.Estilistas.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Estilista creado correctamente.";
        return RedirectToAction("Estilistas");
    }

    // POST /Admin/EditarEstilista
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEstilista(Estilista model)
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
        await _db.SaveChangesAsync();
        TempData["Success"] = "Estilista actualizado.";
        return RedirectToAction("Estilistas");
    }

    // POST /Admin/EliminarEstilista/5
    [HttpPost]
    public async Task<IActionResult> EliminarEstilista(int id)
    {
        var check = AdminOnly(); if (check != null) return check;
        var est = await _db.Estilistas.FindAsync(id);
        if (est != null) { est.Activo = false; await _db.SaveChangesAsync(); }
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