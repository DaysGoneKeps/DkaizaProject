using DkaizaProject.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DkaizaProject.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Estilista> Estilistas { get; set; }
    public DbSet<NotaCliente> NotasCliente { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Cupon> Cupones { get; set; }
    public DbSet<Calificacion> Calificaciones { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<CategoriaServicio> CategoriasServicios { get; set; }
    
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);


        mb.Entity<Cupon>().HasData(
            new Cupon
            {
                Id = 1,
                Codigo = "DKAIZA10",
                PorcentajeDescuento = 10,
                MontoDescuento = 0,
                EsPorcentaje = true,
                Activo = true,
                UsoMaximo = 50,
                UsosActuales = 0,
                Descripcion = "10% de descuento en cualquier servicio",
                FechaExpiracion = new DateTime(2026, 12, 31)
            },
            new Cupon
            {
                Id = 2,
                Codigo = "PROMO20",
                PorcentajeDescuento = 0,
                MontoDescuento = 20,
                EsPorcentaje = false,
                Activo = true,
                UsoMaximo = 30,
                UsosActuales = 0,
                Descripcion = "S/20 de descuento fijo",
                FechaExpiracion = new DateTime(2026, 12, 31)
            }
        );

        // Configurar relación Servicio - Categoria
        mb.Entity<Servicio>()
            .HasOne(s => s.Categoria)
            .WithMany(c => c.Servicios)
            .HasForeignKey(s => s.CategoriaServicioId)
            .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Cita>()
            .HasOne(c => c.Cliente)
            .WithMany(cl => cl.Citas)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Cita>()
            .HasOne(c => c.Servicio)
            .WithMany(s => s.Citas)
            .HasForeignKey(c => c.ServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Cita>()
            .HasOne(c => c.Estilista)
            .WithMany(e => e.Citas)
            .HasForeignKey(c => c.EstilistaId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Pago>()
            .HasOne(p => p.Cita)
            .WithOne(c => c.Pago)
            .HasForeignKey<Pago>(p => p.CitaId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Cliente>()
            .HasOne(c => c.Estilista)
            .WithMany()
            .HasForeignKey(c => c.EstilistaId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==================== SEED DATA ====================
        
        // 1. Categorías de Servicios
        mb.Entity<CategoriaServicio>().HasData(
            new CategoriaServicio { Id = 1, Nombre = "Cabello", Descripcion = "Cortes, peinados y tratamientos capilares", Icono = "fa-cut", Activo = true, Orden = 1 },
            new CategoriaServicio { Id = 2, Nombre = "Coloración", Descripcion = "Tintes, mechas y balayage", Icono = "fa-palette", Activo = true, Orden = 2 },
            new CategoriaServicio { Id = 3, Nombre = "Tratamientos", Descripcion = "Keratina, hidratación y más", Icono = "fa-spa", Activo = true, Orden = 3 },
            new CategoriaServicio { Id = 4, Nombre = "Peinados", Descripcion = "Recogidos, ondas y planchado", Icono = "fa-hand-sparkles", Activo = true, Orden = 4 },
            new CategoriaServicio { Id = 5, Nombre = "Manos y Pies", Descripcion = "Manicure y pedicure profesional", Icono = "fa-hand-peace", Activo = true, Orden = 5 },
            new CategoriaServicio { Id = 6, Nombre = "Maquillaje", Descripcion = "Maquillaje social y profesional", Icono = "fa-brush", Activo = true, Orden = 6 }
        );

        // 2. Servicios enlazados a categorías
        mb.Entity<Servicio>().HasData(
            // Categoría CABELLO (Id 1)
            new Servicio { Id = 1, Nombre = "Corte de cabello", Descripcion = "Corte personalizado según tu estilo y tipo de cabello", DuracionHoras = 1, Precio = 35, Activo = true, CategoriaServicioId = 1 },
            new Servicio { Id = 7, Nombre = "Corte infantil", Descripcion = "Corte especial para niños hasta 12 años", DuracionHoras = 1, Precio = 25, Activo = true, CategoriaServicioId = 1 },
            new Servicio { Id = 8, Nombre = "Corte con navaja", Descripcion = "Técnica de corte con navaja para texturizar", DuracionHoras = 1, Precio = 45, Activo = true, CategoriaServicioId = 1 },
            
            // Categoría COLORACIÓN (Id 2)
            new Servicio { Id = 2, Nombre = "Tinte completo", Descripcion = "Coloración completa con productos de alta calidad", DuracionHoras = 2, Precio = 90, Activo = true, CategoriaServicioId = 2 },
            new Servicio { Id = 3, Nombre = "Mechas / Balayage", Descripcion = "Técnica de iluminación para un look natural", DuracionHoras = 2, Precio = 110, Activo = true, CategoriaServicioId = 2 },
            new Servicio { Id = 9, Nombre = "Reflejos", Descripcion = "Mechas finas para dar luminosidad", DuracionHoras = 2, Precio = 95, Activo = true, CategoriaServicioId = 2 },
            new Servicio { Id = 10, Nombre = "Color fantasía", Descripcion = "Tintes en colores vibrantes (rosa, azul, morado)", DuracionHoras = 3, Precio = 130, Activo = true, CategoriaServicioId = 2 },
            new Servicio { Id = 11, Nombre = "Matizado", Descripcion = "Neutralización de tonos no deseados", DuracionHoras = 1, Precio = 50, Activo = true, CategoriaServicioId = 2 },
            
            // Categoría TRATAMIENTOS (Id 3)
            new Servicio { Id = 4, Nombre = "Alisado keratina", Descripcion = "Tratamiento alisante que nutre y repara", DuracionHoras = 2, Precio = 130, Activo = true, CategoriaServicioId = 3 },
            new Servicio { Id = 12, Nombre = "Hidratación profunda", Descripcion = "Tratamiento intensivo para cabello seco", DuracionHoras = 1, Precio = 45, Activo = true, CategoriaServicioId = 3 },
            new Servicio { Id = 13, Nombre = "Botox capilar", Descripcion = "Tratamiento reconstructivo antioxidante", DuracionHoras = 2, Precio = 120, Activo = true, CategoriaServicioId = 3 },
            new Servicio { Id = 14, Nombre = "Reconstrucción capilar", Descripcion = "Reparación de cabello dañado químicamente", DuracionHoras = 2, Precio = 110, Activo = true, CategoriaServicioId = 3 },
            
            // Categoría PEINADOS (Id 4)
            new Servicio { Id = 5, Nombre = "Lavado y peinado", Descripcion = "Lavado + peinado profesional", DuracionHoras = 1, Precio = 40, Activo = true, CategoriaServicioId = 4 },
            new Servicio { Id = 15, Nombre = "Peinado de novia", Descripcion = "Recogido o semirecogido para ocasiones especiales", DuracionHoras = 2, Precio = 80, Activo = true, CategoriaServicioId = 4 },
            new Servicio { Id = 16, Nombre = "Ondas y rulos", Descripcion = "Ondas definidas o sueltas según tu preferencia", DuracionHoras = 1, Precio = 50, Activo = true, CategoriaServicioId = 4 },
            new Servicio { Id = 17, Nombre = "Planchado profesional", Descripcion = "Alisado con plancha y protección térmica", DuracionHoras = 1, Precio = 35, Activo = true, CategoriaServicioId = 4 },
            
            // Categoría MANOS Y PIES (Id 5)
            new Servicio { Id = 18, Nombre = "Manicure clásico", Descripcion = "Limpieza, corte y esmaltado", DuracionHoras = 1, Precio = 35, Activo = true, CategoriaServicioId = 5 },
            new Servicio { Id = 19, Nombre = "Pedicure clásico", Descripcion = "Cuidado completo de pies", DuracionHoras = 1, Precio = 40, Activo = true, CategoriaServicioId = 5 },
            new Servicio { Id = 20, Nombre = "Manicure con gelish", Descripcion = "Esmaltado semipermanente en uñas", DuracionHoras = 1, Precio = 50, Activo = true, CategoriaServicioId = 5 },
            new Servicio { Id = 21, Nombre = "Pedicure con gelish", Descripcion = "Esmaltado semipermanente en pies", DuracionHoras = 1, Precio = 55, Activo = true, CategoriaServicioId = 5 },
            new Servicio { Id = 22, Nombre = "Combo Manicure + Pedicure", Descripcion = "Ambos servicios con descuento especial", DuracionHoras = 2, Precio = 65, Activo = true, CategoriaServicioId = 5 },
            
            // Categoría MAQUILLAJE (Id 6)
            new Servicio { Id = 23, Nombre = "Maquillaje social", Descripcion = "Maquillaje para eventos y ocasiones especiales", DuracionHoras = 1, Precio = 60, Activo = true, CategoriaServicioId = 6 },
            new Servicio { Id = 24, Nombre = "Maquillaje de novia", Descripcion = "Maquillaje profesional para tu día especial", DuracionHoras = 2, Precio = 120, Activo = true, CategoriaServicioId = 6 },
            new Servicio { Id = 25, Nombre = "Maquillaje artístico", Descripcion = "Diseños creativos y caracterización", DuracionHoras = 2, Precio = 90, Activo = true, CategoriaServicioId = 6 },
            
            // Combo especial (sin categoría específica, puede ir en Tratamientos o destacado)
            new Servicio { Id = 6, Nombre = "Tratamiento completo", Descripcion = "Tinte + corte + peinado", DuracionHoras = 3, Precio = 160, Activo = true, CategoriaServicioId = 3 }
        );

        // 3. Estilistas
        mb.Entity<Estilista>().HasData(
    new Estilista { Id = 1, Nombre = "María González", Especialidad = "Coloración y Balayage", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 12, HoraFinDescanso = 13, Activo = true },
    new Estilista { Id = 2, Nombre = "Laura Fernández", Especialidad = "Cortes y Peinados", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 13, HoraFinDescanso = 14, Activo = true },
    new Estilista { Id = 3, Nombre = "Carolina Rojas", Especialidad = "Tratamientos y Keratina", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 15, HoraFinDescanso = 16, Activo = true }
);

        // 4. Admin user seed (contraseña: Admin123!)
        mb.Entity<Cliente>().HasData(
            new Cliente
            {
                Id = 1,
                Nombre = "Admin",
                Apellido = "Salon",
                Email = "admin@salon.com",
                Telefono = "999-999-999",
                PasswordHash = "$2a$11$eC3MeRvFgm5TqxeMK4xxYuZGPth0aElF2fqklF.G/mQEVZJ3fsbwK",
                EsAdmin = true,
                FechaRegistro = new DateTime(2024, 1, 1)
            },
            new Cliente
            {
                Id = 1000,
                Nombre = "Recepcion",
                Apellido = "Dkaiza",
                Email = "recepcion@dkaiza.com",
                Telefono = "",
                PasswordHash = "$2a$11$rImcKUDCn6N6xavnKyabG.NFBP/BWszxCHLiu3IPkVii7A6JUWEC6",
                EsAdmin = false,
                EsRecepcionista = true,
                FechaRegistro = new DateTime(2024, 1, 1)
            }
        );

        mb.Entity<Calificacion>()
            .HasOne(c => c.Cita)
            .WithOne()
            .HasForeignKey<Calificacion>(c => c.CitaId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Calificacion>()
            .HasOne(c => c.Estilista)
            .WithMany(e => e.Calificaciones)
            .HasForeignKey(c => c.EstilistaId)
            .OnDelete(DeleteBehavior.NoAction);

        mb.Entity<Calificacion>()
            .HasOne(c => c.Cliente)
            .WithMany()
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}