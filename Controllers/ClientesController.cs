using Microsoft.AspNetCore.Mvc;
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
    }
}