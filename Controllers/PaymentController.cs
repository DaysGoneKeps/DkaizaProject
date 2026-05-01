using System.Text.Json;
using DkaizaProject.Data;
using DkaizaProject.Models;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DkaizaProject.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly MercadoPagoSettings _mp;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext db, IOptionsSnapshot<MercadoPagoSettings> mp, IConfiguration config)
        {
            _db = db;
            _config = config;
            _mp = mp.Value;
            _mp.AccessToken = (_mp.AccessToken ?? string.Empty).Trim();
            _mp.PublicKey = (_mp.PublicKey ?? string.Empty).Trim();
            _mp.Currency = string.IsNullOrWhiteSpace(_mp.Currency) ? "PEN" : _mp.Currency.Trim();

            var raw = (_config["MercadoPago:AccessToken"] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(_mp.AccessToken) && !string.IsNullOrEmpty(raw))
                _mp.AccessToken = raw;
        }

        private int? CurrentClienteId => HttpContext.Session.GetInt32("ClienteId");

        private ReservaPendiente? GetReservaPendiente()
        {
            var raw = HttpContext.Session.GetString(AppointmentsController.ReservaPendienteSessionKey);
            return string.IsNullOrEmpty(raw) ? null : JsonSerializer.Deserialize<ReservaPendiente>(raw);
        }

        // GET /Payment/Checkout
        public async Task<IActionResult> Checkout()
        {
            if (CurrentClienteId == null)
                return RedirectToAction("Login", "Account");

            var pendiente = GetReservaPendiente();
            if (pendiente == null || pendiente.ClienteId != CurrentClienteId.Value)
                return RedirectToAction("Servicios", "Appointments");

            var servicio = await _db.Servicios.FindAsync(pendiente.ServicioId);
            var estilista = await _db.Estilistas.FindAsync(pendiente.EstilistaId);
            if (servicio == null || estilista == null)
                return RedirectToAction("Servicios", "Appointments");

            decimal porcentaje = Math.Clamp(_mp.PorcentajeSenal, 1, 100);
            decimal montoSenal = Math.Round(servicio.Precio * porcentaje / 100m, 2);
            bool configOk = !string.IsNullOrWhiteSpace(_mp.AccessToken)
                && !_mp.AccessToken.StartsWith("PEGA_AQUI");

            ViewBag.TokenLen = _mp.AccessToken?.Length ?? 0;
            ViewBag.TokenPrefix = string.IsNullOrEmpty(_mp.AccessToken)
                ? "(vacio)"
                : _mp.AccessToken.Substring(0, Math.Min(12, _mp.AccessToken.Length));

            var vm = new CheckoutViewModel
            {
                Servicio = servicio,
                Estilista = estilista,
                Fecha = pendiente.Fecha,
                HoraInicio = pendiente.HoraInicio,
                HoraFin = pendiente.HoraFin,
                MontoTotal = servicio.Precio,
                MontoSenal = montoSenal,
                PorcentajeSenal = (int)porcentaje,
                Currency = string.IsNullOrWhiteSpace(_mp.Currency) ? "PEN" : _mp.Currency,
                ConfigurationOk = configOk
            };
            return View(vm);
        }

        // POST /Payment/Iniciar - crea preferencia y redirige a MercadoPago
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar()
        {
            if (CurrentClienteId == null)
                return RedirectToAction("Login", "Account");

            var pendiente = GetReservaPendiente();
            if (pendiente == null || pendiente.ClienteId != CurrentClienteId.Value)
                return RedirectToAction("Servicios", "Appointments");

            if (string.IsNullOrWhiteSpace(_mp.AccessToken) || _mp.AccessToken.StartsWith("PEGA_AQUI"))
            {
                TempData["PaymentError"] = "MercadoPago no está configurado. Pega tu Access Token de TEST en appsettings.json (sección MercadoPago).";
                return RedirectToAction(nameof(Checkout));
            }

            var servicio = await _db.Servicios.FindAsync(pendiente.ServicioId);
            var estilista = await _db.Estilistas.FindAsync(pendiente.EstilistaId);
            if (servicio == null || estilista == null)
                return RedirectToAction("Servicios", "Appointments");

            decimal porcentaje = Math.Clamp(_mp.PorcentajeSenal, 1, 100);
            decimal montoSenal = Math.Round(servicio.Precio * porcentaje / 100m, 2);
            string currency = string.IsNullOrWhiteSpace(_mp.Currency) ? "PEN" : _mp.Currency;
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            MercadoPagoConfig.AccessToken = _mp.AccessToken;
            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Id = pendiente.ExternalReference,
                        Title = $"Señal {porcentaje}% - {servicio.Nombre} con {estilista.Nombre}",
                        Description = $"Reserva {pendiente.Fecha:dd/MM/yyyy} {pendiente.HoraInicio:D2}:00",
                        Quantity = 1,
                        CurrencyId = currency,
                        UnitPrice = montoSenal
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{baseUrl}/Payment/Success",
                    Failure = $"{baseUrl}/Payment/Failure",
                    Pending = $"{baseUrl}/Payment/Pending"
                },
                ExternalReference = pendiente.ExternalReference
            };

            try
            {
                var client = new PreferenceClient();
                var preference = await client.CreateAsync(request);
                HttpContext.Session.SetString("PreferenceId", preference.Id);
                HttpContext.Session.SetString("MontoSenalPendiente", montoSenal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return Redirect(preference.InitPoint);
            }
            catch (Exception ex)
            {
                TempData["PaymentError"] = "No se pudo iniciar el pago: " + ex.Message;
                return RedirectToAction(nameof(Checkout));
            }
        }

        // GET /Payment/Success - MercadoPago redirige aquí tras aprobación
        public async Task<IActionResult> Success(string? payment_id, string? status, string? external_reference, string? payment_type)
        {
            if (CurrentClienteId == null)
                return RedirectToAction("Login", "Account");

            var pendiente = GetReservaPendiente();
            if (pendiente == null || pendiente.ClienteId != CurrentClienteId.Value)
            {
                ViewBag.Mensaje = "No encontramos los datos de tu reserva. Por favor inicia el proceso nuevamente.";
                return View("Failure");
            }

            if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Pending), new { status });

            var servicio = await _db.Servicios.FindAsync(pendiente.ServicioId);
            var estilista = await _db.Estilistas.FindAsync(pendiente.EstilistaId);
            if (servicio == null || estilista == null)
                return RedirectToAction("Servicios", "Appointments");

            var conflicto = await _db.Citas.AnyAsync(c =>
                c.Fecha.Date == pendiente.Fecha.Date &&
                c.EstilistaId == pendiente.EstilistaId &&
                c.Estado != EstadoCita.Cancelada &&
                c.HoraInicio < pendiente.HoraFin &&
                c.HoraFin > pendiente.HoraInicio);
            if (conflicto)
            {
                ViewBag.Mensaje = "El horario fue tomado mientras realizabas el pago. Contacta al salón para resolverlo o reintenta con otro horario.";
                return View("Failure");
            }

            var cita = new Cita
            {
                ClienteId = pendiente.ClienteId,
                ServicioId = pendiente.ServicioId,
                EstilistaId = pendiente.EstilistaId,
                Fecha = pendiente.Fecha,
                HoraInicio = pendiente.HoraInicio,
                HoraFin = pendiente.HoraFin,
                Notas = pendiente.Notas,
                Estado = EstadoCita.Confirmada
            };
            _db.Citas.Add(cita);
            await _db.SaveChangesAsync();

            decimal montoSenal = 0m;
            var montoRaw = HttpContext.Session.GetString("MontoSenalPendiente");
            if (!string.IsNullOrEmpty(montoRaw))
                decimal.TryParse(montoRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out montoSenal);

            var pago = new Pago
            {
                CitaId = cita.Id,
                PreferenceId = HttpContext.Session.GetString("PreferenceId"),
                PaymentId = payment_id,
                ExternalReference = pendiente.ExternalReference,
                Monto = montoSenal,
                MontoTotal = servicio.Precio,
                Metodo = payment_type,
                Estado = EstadoPago.Aprobado,
                FechaPago = DateTime.Now
            };
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(AppointmentsController.ReservaPendienteSessionKey);
            HttpContext.Session.Remove("PreferenceId");
            HttpContext.Session.Remove("MontoSenalPendiente");

            ViewBag.Resumen = $"{servicio.Nombre} con {estilista.Nombre} el {cita.Fecha:dd/MM/yyyy} de {cita.HoraInicio:D2}:00 a {cita.HoraFin:D2}:00";
            ViewBag.MontoPagado = montoSenal;
            ViewBag.MontoTotal = servicio.Precio;
            ViewBag.PaymentId = payment_id;
            return View();
        }

        // GET /Payment/Failure
        public IActionResult Failure()
        {
            ViewBag.Mensaje ??= "El pago no se pudo completar. Tu reserva no fue confirmada.";
            return View();
        }

        // GET /Payment/Pending
        public IActionResult Pending(string? status)
        {
            ViewBag.Status = status;
            return View();
        }
    }
}
