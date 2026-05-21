(function () {
    var token = function () {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    };

    function mensaje(texto, tipo) {
        var banner = document.getElementById('mensajeBanner');
        banner.textContent = texto;
        banner.className = 'mensaje-banner shown ' + (tipo === 'ok' ? 'ok' : 'err');
        setTimeout(function () { banner.classList.remove('shown'); }, 4000);
    }

    function badgeFor(estado) {
        if (estado === 'Pagada') return { cls: 'badge-pagada', text: 'Pagada' };
        if (estado === 'Pendiente') return { cls: 'badge-pendiente', text: 'Pendiente' };
        return { cls: 'badge-otro', text: estado };
    }

    window.buscarCita = function () {
        var codigo = parseInt(document.getElementById('codigoCita').value, 10);
        if (!codigo) { mensaje('Ingrese un código válido', 'err'); return; }

        var fd = new FormData();
        fd.append('codigo', codigo);
        fd.append('__RequestVerificationToken', token());

        fetch('/Recepcionista/BuscarCita', { method: 'POST', body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.success) { mensaje(data.message || 'No se encontró', 'err'); document.getElementById('panelCita').classList.remove('shown'); return; }
                var c = data.cita;
                document.getElementById('citaServicio').textContent = c.servicio;
                document.getElementById('citaSub').textContent = 'Cita #' + c.id;
                var b = badgeFor(c.estadoCita);
                var badge = document.getElementById('citaBadge');
                badge.className = 'badge ' + b.cls;
                badge.textContent = b.text;
                document.getElementById('citaCliente').textContent = c.cliente;
                document.getElementById('citaTelefono').textContent = c.telefono || '—';
                document.getElementById('citaFecha').textContent = c.fecha;
                document.getElementById('citaHorario').textContent = c.horario;
                document.getElementById('citaEstilista').textContent = c.estilista;
                document.getElementById('citaPrecio').textContent = 'S/ ' + Number(c.precio).toFixed(2);

                var section = document.getElementById('pagoSection');
                if (!c.tienePago) {
                    section.innerHTML = ''
                        + '<h4 style="font-family: var(--font-display); font-size: 1rem; margin: 0 0 0.75rem 0;">Registrar pago</h4>'
                        + '<form class="form-pago" onsubmit="event.preventDefault(); registrarPago(' + c.id + ');">'
                        + '  <div><label>Monto (S/)</label><input type="number" step="0.01" min="0" id="regMonto" value="' + c.precio + '" required /></div>'
                        + '  <div><label>Método</label><select id="regMetodo"><option value="Efectivo">Efectivo</option><option value="Tarjeta">Tarjeta</option><option value="Yape">Yape</option><option value="Plin">Plin</option><option value="Transferencia">Transferencia</option></select></div>'
                        + '  <div><button type="submit" class="btn-rec"><i class="fas fa-save"></i> Registrar</button></div>'
                        + '</form>';
                } else if (!c.pago.validado) {
                    section.innerHTML = ''
                        + '<h4 style="font-family: var(--font-display); font-size: 1rem; margin: 0 0 0.75rem 0;">Pago pendiente de validar</h4>'
                        + '<div class="info-grid">'
                        + '  <div class="info-cell"><div class="l">Monto</div><div class="v">S/ ' + Number(c.pago.monto).toFixed(2) + '</div></div>'
                        + '  <div class="info-cell"><div class="l">Método</div><div class="v">' + (c.pago.metodo || '—') + '</div></div>'
                        + '</div>'
                        + '<button class="btn-rec green" onclick="validarPago(' + c.pago.id + ')"><i class="fas fa-check"></i> Validar pago</button>';
                } else {
                    section.innerHTML = ''
                        + '<h4 style="font-family: var(--font-display); font-size: 1rem; margin: 0 0 0.75rem 0;">Pago validado</h4>'
                        + '<div class="info-grid">'
                        + '  <div class="info-cell"><div class="l">N° operación</div><div class="v">' + (c.pago.numeroOperacion || '—') + '</div></div>'
                        + '  <div class="info-cell"><div class="l">Monto</div><div class="v">S/ ' + Number(c.pago.monto).toFixed(2) + '</div></div>'
                        + '  <div class="info-cell"><div class="l">Método</div><div class="v">' + (c.pago.metodo || '—') + '</div></div>'
                        + '</div>';
                }
                document.getElementById('panelCita').classList.add('shown');
            })
            .catch(function () { mensaje('Error en la solicitud', 'err'); });
    };

    window.registrarPago = function (citaId) {
        var monto = parseFloat(document.getElementById('regMonto').value);
        var metodo = document.getElementById('regMetodo').value;
        var fd = new FormData();
        fd.append('citaId', citaId);
        fd.append('monto', monto);
        fd.append('metodo', metodo);
        fd.append('__RequestVerificationToken', token());

        fetch('/Recepcionista/RegistrarPago', { method: 'POST', body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    mensaje(data.message, 'ok');
                    setTimeout(function () { location.reload(); }, 800);
                } else {
                    mensaje(data.message || 'Error', 'err');
                }
            })
            .catch(function () { mensaje('Error en la solicitud', 'err'); });
    };

    window.validarPago = function (pagoId) {
        if (!confirm('¿Validar el pago? El estado de la cita pasará a Pagada.')) return;
        var fd = new FormData();
        fd.append('pagoId', pagoId);
        fd.append('__RequestVerificationToken', token());

        fetch('/Recepcionista/ValidarPago', { method: 'POST', body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    mensaje(data.message + ' (N° ' + data.numeroOperacion + ')', 'ok');
                    setTimeout(function () { location.reload(); }, 1000);
                } else {
                    mensaje(data.message || 'Error', 'err');
                }
            })
            .catch(function () { mensaje('Error en la solicitud', 'err'); });
    };

    document.addEventListener('DOMContentLoaded', function () {
        var input = document.getElementById('codigoCita');
        if (input) input.addEventListener('keypress', function (e) { if (e.key === 'Enter') { e.preventDefault(); buscarCita(); } });
    });
})();
