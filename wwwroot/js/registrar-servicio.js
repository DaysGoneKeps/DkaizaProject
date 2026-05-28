(function () {
    var token = function () {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    };

    function msg(texto, tipo) {
        var b = document.getElementById('rsMsg');
        b.textContent = texto;
        b.className = 'rs-msg shown ' + (tipo === 'ok' ? 'ok' : 'err');
        if (tipo !== 'ok') setTimeout(function () { b.classList.remove('shown'); }, 5000);
    }

    function fechaHoy() {
        var d = new Date();
        var m = String(d.getMonth() + 1).padStart(2, '0');
        var dd = String(d.getDate()).padStart(2, '0');
        return d.getFullYear() + '-' + m + '-' + dd;
    }

    function cargarServicios() {
        fetch('/Clientes/ApiServicios')
            .then(function (r) { return r.json(); })
            .then(function (servicios) {
                var grid = document.getElementById('gridServicios');
                if (!servicios.length) { grid.innerHTML = '<div class="picker-empty">No hay servicios activos</div>'; return; }
                grid.innerHTML = servicios.map(function (s) {
                    return '<div class="picker-item" data-id="' + s.id + '" data-precio="' + s.precio + '" onclick="seleccionarServicio(' + s.id + ')">'
                        + '<span class="pi-title">' + s.nombre + '</span>'
                        + '<span class="pi-sub">S/ ' + Number(s.precio).toFixed(2) + ' · ' + s.duracion + 'h</span>'
                        + '</div>';
                }).join('');
            })
            .catch(function () { msg('Error al cargar servicios', 'err'); });
    }

    function cargarEstilistas() {
        fetch('/Clientes/ApiEstilistas')
            .then(function (r) { return r.json(); })
            .then(function (estilistas) {
                var grid = document.getElementById('gridEstilistas');
                if (!estilistas.length) { grid.innerHTML = '<div class="picker-empty">No hay estilistas activos</div>'; return; }
                grid.innerHTML = estilistas.map(function (e) {
                    return '<div class="picker-item" data-id="' + e.id + '" onclick="seleccionarEstilista(' + e.id + ')">'
                        + '<span class="pi-title">' + e.nombre + '</span>'
                        + (e.especialidad ? '<span class="pi-sub">' + e.especialidad + '</span>' : '')
                        + '</div>';
                }).join('');
            })
            .catch(function () { msg('Error al cargar estilistas', 'err'); });
    }

    window.seleccionarServicio = function (id) {
        var items = document.querySelectorAll('#gridServicios .picker-item');
        var label = '';
        items.forEach(function (it) {
            if (parseInt(it.getAttribute('data-id'), 10) === id) {
                it.classList.add('active');
                label = it.querySelector('.pi-title').textContent;
                var precio = it.getAttribute('data-precio');
                if (precio) document.getElementById('monto').value = Number(precio).toFixed(2);
            } else { it.classList.remove('active'); }
        });
        document.getElementById('servicioId').value = id;
        document.getElementById('selServicio').textContent = label ? '· ' + label : '';
        cargarHorarios();
    };

    window.seleccionarEstilista = function (id) {
        var items = document.querySelectorAll('#gridEstilistas .picker-item');
        var label = '';
        items.forEach(function (it) {
            if (parseInt(it.getAttribute('data-id'), 10) === id) {
                it.classList.add('active');
                label = it.querySelector('.pi-title').textContent;
            } else { it.classList.remove('active'); }
        });
        document.getElementById('estilistaId').value = id;
        document.getElementById('selEstilista').textContent = label ? '· ' + label : '';
        cargarHorarios();
    };

    function cargarHorarios() {
        var sId = document.getElementById('servicioId').value;
        var eId = document.getElementById('estilistaId').value;
        var f = document.getElementById('fecha').value;
        var sel = document.getElementById('horaInicio');
        if (!sId || !eId || !f) { sel.innerHTML = '<option value="">Seleccione servicio, estilista y fecha</option>'; return; }
        sel.innerHTML = '<option value="">Cargando…</option>';
        var url = '/Clientes/ApiHorarios?servicioId=' + encodeURIComponent(sId) + '&estilistaId=' + encodeURIComponent(eId) + '&fecha=' + encodeURIComponent(f);
        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.success) { sel.innerHTML = '<option value="">' + (data.message || 'Error') + '</option>'; return; }
                var libres = data.slots.filter(function (s) { return s.disponible; });
                if (!libres.length) { sel.innerHTML = '<option value="">Sin horarios disponibles</option>'; return; }
                sel.innerHTML = '<option value="">Seleccione horario</option>'
                    + libres.map(function (s) { return '<option value="' + s.horaInicio + '">' + s.label + '</option>'; }).join('');
            })
            .catch(function () { sel.innerHTML = '<option value="">Error al cargar horarios</option>'; });
    }

    window.registrarServicio = function () {
        var id = document.getElementById('clienteId').value;
        var servicioId = document.getElementById('servicioId').value;
        var estilistaId = document.getElementById('estilistaId').value;
        var fecha = document.getElementById('fecha').value;
        var horaInicio = document.getElementById('horaInicio').value;
        var monto = document.getElementById('monto').value;
        var metodo = document.getElementById('metodo').value;

        if (!servicioId) { msg('Seleccione un servicio', 'err'); return; }
        if (!estilistaId) { msg('Seleccione un estilista', 'err'); return; }
        if (!fecha) { msg('Seleccione una fecha', 'err'); return; }
        if (!horaInicio) { msg('Seleccione un horario', 'err'); return; }
        if (!monto || parseFloat(monto) <= 0) { msg('Ingrese un monto válido', 'err'); return; }

        var fd = new FormData();
        fd.append('id', id);
        fd.append('servicioId', servicioId);
        fd.append('estilistaId', estilistaId);
        fd.append('fecha', fecha);
        fd.append('horaInicio', horaInicio);
        fd.append('monto', monto);
        fd.append('metodo', metodo);
        fd.append('__RequestVerificationToken', token());

        fetch('/Clientes/RegistrarServicio', { method: 'POST', body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    msg(data.message + ' — Cita #' + data.citaId + '. Redirigiendo al panel para validar el pago…', 'ok');
                    setTimeout(function () { window.location.href = '/Recepcionista'; }, 1500);
                } else {
                    msg(data.message || 'Error', 'err');
                }
            })
            .catch(function () { msg('Error en la solicitud', 'err'); });
    };

    document.addEventListener('DOMContentLoaded', function () {
        document.getElementById('fecha').value = fechaHoy();
        document.getElementById('fecha').addEventListener('change', cargarHorarios);
        cargarServicios();
        cargarEstilistas();
    });
})();
