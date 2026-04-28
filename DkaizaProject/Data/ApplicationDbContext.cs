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
    public DbSet<Cita> Citas { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
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

        // Seed data
        mb.Entity<Servicio>().HasData(
            new Servicio { Id = 1, Nombre = "Corte de cabello", Descripcion = "Corte personalizado", DuracionHoras = 1, Precio = 35 },
            new Servicio { Id = 2, Nombre = "Lavado y peinado", Descripcion = "Lavado + peinado profesional", DuracionHoras = 1, Precio = 40 },
            new Servicio { Id = 3, Nombre = "Tinte completo", Descripcion = "Coloración completa", DuracionHoras = 2, Precio = 90 },
            new Servicio { Id = 4, Nombre = "Mechas / Balayage", Descripcion = "Técnica de iluminación", DuracionHoras = 2, Precio = 110 },
            new Servicio { Id = 5, Nombre = "Alisado keratina", Descripcion = "Tratamiento alisante", DuracionHoras = 2, Precio = 130 },
            new Servicio { Id = 6, Nombre = "Tratamiento completo", Descripcion = "Tinte + corte + peinado", DuracionHoras = 3, Precio = 160 }
        );

        mb.Entity<Estilista>().HasData(
            new Estilista { Id = 1, Nombre = "Estilista 1", Especialidad = "Coloración", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 12, HoraFinDescanso = 13 },
            new Estilista { Id = 2, Nombre = "Estilista 2", Especialidad = "Cortes y Peinados", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 13, HoraFinDescanso = 14 },
            new Estilista { Id = 3, Nombre = "Estilista 3", Especialidad = "Tratamientos", HoraInicioTrabajo = 10, HoraFinTrabajo = 22, HoraInicioDescanso = 15, HoraFinDescanso = 16 }
        );

        // Admin user seed
        mb.Entity<Cliente>().HasData(
            new Cliente
            {
                Id = 1,
                Nombre = "Admin",
                Apellido = "Salon",
                Email = "admin@salon.com",
                Telefono = "000-000-0000",
                PasswordHash = "$2a$11$uC7VxVY1z8FQqKqgk7y0eOqvYhG9mG8Qk3lYvZlPpQwF8Wz0lZr7K",
                EsAdmin = true,
                FechaRegistro = new DateTime(2024, 1, 1)
            }
        );
    }
}
