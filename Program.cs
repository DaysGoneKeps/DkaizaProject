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

    // Reconciliar historial: si el esquema fisico ya tiene columnas/tablas que
    // pertenecen a SyncModel (porque se aplicaron antes con una migracion que
    // luego fue eliminada), registramos esa migracion como aplicada para evitar
    // el error "duplicate column name" al re-correrla.
    var conn = db.Database.GetDbConnection();
    conn.Open();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "DELETE FROM __EFMigrationsLock";
        try { cmd.ExecuteNonQuery(); } catch { }

        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Pagos') WHERE name = 'CuponCodigo'";
        var yaExiste = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        if (yaExiste)
        {
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260610060352_SyncModel', '9.0.0')";
            cmd.ExecuteNonQuery();
        }
    }
    conn.Close();

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