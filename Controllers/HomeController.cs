using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DkaizaProject.Models;
using DkaizaProject.Data;
using Microsoft.EntityFrameworkCore;

namespace DkaizaProject.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    // Página principal (Index)
    public async Task<IActionResult> Index()
    {
        var servicios = await _db.Servicios.Where(s => s.Activo).ToListAsync();
        return View("~/Views/Home/Index.cshtml", servicios);
    }

    // Página para explorar servicios (todos los servicios)
    public async Task<IActionResult> Servicios()
    {
        var servicios = await _db.Servicios
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .ToListAsync();
        
        return View("~/Views/Home/Servicios.cshtml", servicios);
    }

    public IActionResult Nosotros()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}