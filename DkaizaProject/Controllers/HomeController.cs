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

    public async Task<IActionResult> Index()
    {
        var servicios = await _db.Servicios.Where(s => s.Activo).ToListAsync();
        
        // Forzar explícitamente tu vista
        return View("~/Views/Home/Index.cshtml", servicios);
    }
}