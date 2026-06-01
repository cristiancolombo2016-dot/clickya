(function () {
    const sidebar = document.getElementById("cySidebar");
    const overlay = document.getElementById("cyOverlay");
    const btnOpen = document.getElementById("cyMenuBtn");
    const btnClose = document.getElementById("cyCloseBtn");
    if (!sidebar || !overlay || !btnOpen) return;
    function openMenu() { sidebar.classList.add("open"); overlay.classList.add("show"); document.body.style.overflow = "hidden"; }
    function closeMenu() { sidebar.classList.remove("open"); overlay.classList.remove("show"); document.body.style.overflow = ""; }
    btnOpen.addEventListener("click", openMenu);
    overlay.addEventListener("click", closeMenu);
    if (btnClose) btnClose.addEventListener("click", closeMenu);
    window.addEventListener("resize", function () { if (window.innerWidth > 992) closeMenu(); });
})();

document.addEventListener("click", function (e) {
    if (e.target.closest("#btnEditarPerfil")) {
        const modal = document.getElementById("modalEditarPerfil");
        const sidebar = document.getElementById("cySidebar");
        const overlay = document.getElementById("cyOverlay");
        if (sidebar) sidebar.classList.remove("open");
        if (overlay) overlay.classList.remove("show");
        document.body.style.overflow = "";
        cargarPerfil();
        if (modal) modal.style.display = "flex";
        e.preventDefault();
    }
    if (e.target.closest("#btnCerrarPerfil")) {
        const modal = document.getElementById("modalEditarPerfil");
        if (modal) modal.style.display = "none";
    }
});

function aplicarPerfilEnVista(perfil) {
    if (document.getElementById('txtNombre')) document.getElementById('txtNombre').textContent = perfil.nombre || 'Nombre del comercio';
    if (document.getElementById('txtDesc')) document.getElementById('txtDesc').textContent = perfil.descripcion || '';
    if (document.getElementById('linkWhatsapp')) document.getElementById('linkWhatsapp').href = perfil.whatsapp ? `https://wa.me/${perfil.whatsapp}` : '#';
    if (document.getElementById('linkInstagram')) document.getElementById('linkInstagram').href = perfil.instagram ? `https://instagram.com/${perfil.instagram}` : '#';
    if (document.getElementById('linkUbicacion')) document.getElementById('linkUbicacion').href = perfil.ubicacion ? `https://www.google.com/maps/search/${encodeURIComponent(perfil.ubicacion)}` : '#';
}

function obtenerHorariosTexto() {
    const dias = document.querySelectorAll('.horario-dia');
    let resultado = [];
    dias.forEach(dia => {
        const check = dia.querySelector('input[type=checkbox]');
        const horas = dia.querySelectorAll('input[type=time]');
        if (!check || !check.checked) return;
        const desde = horas[0]?.value;
        const hasta = horas[1]?.value;
        if (desde && hasta) resultado.push(`${check.parentElement.innerText.trim()} ${desde}–${hasta}`);
    });
    return resultado.length > 0 ? resultado.join(' | ') : 'Horarios no informados';
}

function guardarPerfil() {
    guardarDatosPerfil();
}

async function guardarDatosPerfil() {
    const cidPerfil = window.clickyaComercioId || 1;
    const ubicacion = document.getElementById('inpUbicacion')?.value || '';

    let latitud = 0;
    let longitud = 0;

    if (ubicacion && !ubicacion.startsWith('http')) {
        try {
            const geo = await fetch(
                `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(ubicacion + ', San Nicolas, Buenos Aires')}&format=json&limit=1`
            );
            const geoData = await geo.json();
            console.log('GEO RESULT:', geoData);
            if (geoData.length > 0) {
                latitud = parseFloat(geoData[0].lat);
                longitud = parseFloat(geoData[0].lon);
                console.log('LAT:', latitud, 'LNG:', longitud);
            }
        } catch (e) { console.error('GEO ERROR:', e); }
    }

    const perfil = {
        Id: cidPerfil,
        Nombre: document.getElementById('inpNombre')?.value || '',
        Descripcion: document.getElementById('inpDescripcion')?.value || '',
        WhatsApp: document.getElementById('inpWhatsapp')?.value || '',
        Instagram: document.getElementById('inpInstagram')?.value || '',
        Ubicacion: ubicacion,
        Correo: document.getElementById('inpCorreo')?.value || '',
        Horarios: obtenerHorariosTexto(),
        Rubro: document.getElementById('txtRubro')?.innerText || "comida",
        Estado: "Activo",
        Latitud: latitud,
        Longitud: longitud
    };
    fetch(`https://clickya-production.up.railway.app/api/Comercio/${cidPerfil}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(perfil)
    })
        .then(res => { if (!res.ok) throw new Error("Error guardando perfil"); return res.json(); })
        .then(() => {
            aplicarPerfilEnVista({ nombre: perfil.Nombre, descripcion: perfil.Descripcion, whatsapp: perfil.WhatsApp, instagram: perfil.Instagram, ubicacion: perfil.Ubicacion });
            const modal = document.getElementById('modalEditarPerfil');
            if (modal) modal.style.display = 'none';
        })
        .catch(err => { console.error(err); alert("Error guardando perfil"); });
}

function cargarPerfil() {
    const cidCarga = window.clickyaComercioId || 1;
    fetch(`https://clickya-production.up.railway.app/api/Comercio/${cidCarga}`)
        .then(res => res.json())
        .then(perfil => {
            if (document.getElementById('inpNombre')) document.getElementById('inpNombre').value = perfil.nombre || '';
            if (document.getElementById('inpDescripcion')) document.getElementById('inpDescripcion').value = perfil.descripcion || '';
            if (document.getElementById('inpWhatsapp')) document.getElementById('inpWhatsapp').value = perfil.whatsApp || '';
            if (document.getElementById('inpInstagram')) document.getElementById('inpInstagram').value = perfil.instagram || '';
            if (document.getElementById('inpUbicacion')) document.getElementById('inpUbicacion').value = perfil.ubicacion || '';
            if (document.getElementById('inpCorreo')) document.getElementById('inpCorreo').value = perfil.correo || '';
            if (perfil.logoUrl && document.getElementById('previewLogo'))
                document.getElementById('previewLogo').src = "https://clickya-production.up.railway.app" + perfil.logoUrl + "?v=" + Date.now();
        })
        .catch(err => console.error("Error cargando perfil:", err));
}