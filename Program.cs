using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DkaizaProject.Data;
using DkaizaProject.Models;
using DkaizaProject.Services.IA;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<DkaizaProject.Services.RecordatorioCitasService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IChatAiService, OllamaChatService>();
// Configuración de MercadoPago
builder.Services.Configure<MercadoPagoSettings>(
    builder.Configuration.GetSection("MercadoPago"));

// Configuración de sesión (MEMORIA)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

// Configuración de base de datos (UNA SOLA VEZ)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity (si lo estás usando)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Aplicar migraciones pendientes automaticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ===== IMPORTANTE: ACTIVAR EL MIDDLEWARE DE SESIÓN =====
app.UseSession();  // <-- ESTO ES LO QUE FALTABA
// ====================================================

app.UseAuthorization();


app.MapGet("/", () => Results.Redirect("/Home/Index"));


app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
  

app.MapRazorPages()
   .WithStaticAssets();
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("admin123"));

app.Run();