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
    public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;

    public AccountController(ApplicationDbContext db) => _db = db;

    // GET /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32("ClienteId") != null)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Email == model.Email);

        if (cliente == null || !BCrypt.Net.BCrypt.Verify(model.Password, cliente.PasswordHash))
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(model);
        }

        HttpContext.Session.SetInt32("ClienteId", cliente.Id);
        HttpContext.Session.SetString("ClienteNombre", cliente.NombreCompleto);
        HttpContext.Session.SetString("EsAdmin", cliente.EsAdmin.ToString());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Register
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("ClienteId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var exists = await _db.Clientes.AnyAsync(c => c.Email == model.Email);
        if (exists)
        {
            ModelState.AddModelError("Email", "Ya existe una cuenta con ese email.");
            return View(model);
        }

        var cliente = new Cliente
        {
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Email = model.Email,
            Telefono = model.Telefono,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        HttpContext.Session.SetInt32("ClienteId", cliente.Id);
        HttpContext.Session.SetString("ClienteNombre", cliente.NombreCompleto);
        HttpContext.Session.SetString("EsAdmin", "False");

        TempData["Success"] = $"¡Bienvenida/o, {cliente.Nombre}! Tu cuenta fue creada exitosamente.";
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null)
            return RedirectToAction("Login");
        
        var cliente = await _db.Clientes.FindAsync(clienteId);
        if (cliente == null)
            return RedirectToAction("Login");
        
        var perfilVm = new PerfilViewModel
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Apellido = cliente.Apellido,
            Email = cliente.Email,
            Telefono = cliente.Telefono,
            FechaRegistro = cliente.FechaRegistro
        };
        
        return View(perfilVm);
    }
    

    // POST: /Account/ActualizarPerfil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPerfil(PerfilViewModel model)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null)
            return RedirectToAction("Login");
        
        if (!ModelState.IsValid)
            return View("Perfil", model);
        
        var cliente = await _db.Clientes.FindAsync(clienteId);
        if (cliente == null)
            return RedirectToAction("Login");
        
        // Actualizar datos básicos
        cliente.Nombre = model.Nombre;
        cliente.Apellido = model.Apellido;
        cliente.Email = model.Email;
        cliente.Telefono = model.Telefono;
        
        // Actualizar contraseña si se proporcionó
        if (!string.IsNullOrEmpty(model.NuevaPassword))
        {
            cliente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        }
        
        // Actualizar sesión con el nuevo nombre
        HttpContext.Session.SetString("ClienteNombre", cliente.Nombre);
        
        await _db.SaveChangesAsync();
        TempData["Success"] = "Tu perfil ha sido actualizado correctamente";
        
        return RedirectToAction("Perfil");
    }
}

}