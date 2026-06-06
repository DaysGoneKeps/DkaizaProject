using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DkaizaProject.Data;
using DkaizaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DkaizaProject.Services
{
    public class RecordatorioCitasService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<RecordatorioCitasService> _logger;
 
        // Intervalo de chequeo: cada 30 minutos
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(10);
 
        public RecordatorioCitasService(
            IServiceProvider services,
            ILogger<RecordatorioCitasService> logger)
        {
            _services = services;
            _logger = logger;
        }
 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecordatorioCitasService iniciado.");
 
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerarRecordatoriosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en RecordatorioCitasService");
                }
 
                await Task.Delay(Intervalo, stoppingToken);
            }
        }
 
        private async Task GenerarRecordatoriosAsync()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
 
            var ahora = DateTime.Now;
            // Ventana: citas que ocurren entre 23h y 25h desde ahora
            var desde = DateTime.Today;   // cualquier cita que sea en más de 1 minuto
var hasta = ahora.AddHours(48);  
 
            // Citas pendientes o confirmadas dentro de la ventana
            var citas = await db.Citas
                .Include(c => c.Servicio)
                .Include(c => c.Estilista)
                .Where(c =>
                    (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada) &&
                    c.Fecha >= desde &&
                    c.Fecha <= hasta)
                .ToListAsync();
 
            foreach (var cita in citas)
            {
                // Evitar duplicados: ¿ya existe notificación no procesada para esta cita?
                var yaExiste = await db.Notificaciones
                .AnyAsync(n => n.CitaId == cita.Id);
 
                if (yaExiste) continue;
 
                var notif = new Notificacion
                {
                    ClienteId = cita.ClienteId,
                    CitaId = cita.Id,
                    Titulo = "Recordatorio de tu cita",
                    Mensaje = $"Tienes una cita mañana: <strong>{cita.Servicio.Nombre}</strong> " +
                              $"con <strong>{cita.Estilista.Nombre}</strong> " +
                              $"el <strong>{cita.Fecha:dd/MM/yyyy}</strong> " +
                              $"a las <strong>{cita.HoraInicio:D2}:00</strong>. " +
                              $"¿Confirmas tu asistencia?",
                    FechaCreacion = DateTime.UtcNow
                };
 
                db.Notificaciones.Add(notif);
                _logger.LogInformation(
                    "Notificación generada para cita #{CitaId} del cliente #{ClienteId}",
                    cita.Id, cita.ClienteId);
            }
 
            await db.SaveChangesAsync();
        }
    }
}