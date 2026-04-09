let _map = null;
let _markers = {};  // orderNumber -> marker
let _dotNet = null;

async function ensureLeaflet() {
    if (window.L) return;
    // Load Leaflet CSS
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
    document.head.appendChild(link);
    // Load Leaflet JS
    await new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    });
}

window.mapInit = async function (orders, dotNetRef) {
    await ensureLeaflet();
    _dotNet = dotNetRef;

    // Destroy old map
    Object.values(_markers).forEach(m => { try { m.remove(); } catch (e) { } });
    _markers = {};
    if (_map) { try { _map.remove(); } catch (e) { } _map = null; }

    const container = document.getElementById('map-container');
    if (!container) return;

    // Create map centered on Almaty
    _map = L.map('map-container').setView([43.2567, 76.9286], 12);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19
    }).addTo(_map);

    // Geocode and add markers one by one
    for (const order of orders) {
        if (!order.address) continue;
        const coords = await geocode(order.address);
        if (!coords) continue;

        const marker = L.marker([coords[0], coords[1]]).addTo(_map);

        marker.bindPopup(
            `<div style="min-width:180px;font-family:Segoe UI,sans-serif;font-size:13px">
                <div style="font-weight:700;font-size:14px;margin-bottom:4px">№${order.orderNumber}</div>
                <div style="font-weight:600;margin-bottom:4px">${order.address}</div>
                ${order.phone ? '<div>📞 ' + order.phone + '</div>' : ''}
                ${order.amount ? '<div>💰 ' + order.amount + ' ₸</div>' : ''}
                ${order.date ? '<div>📅 ' + order.date + '</div>' : ''}
                ${order.comments ? '<div style="margin-top:4px;color:#555">💬 ' + order.comments + '</div>' : ''}
             </div>`
        );

        const num = order.orderNumber;
        marker.on('click', () => {
            if (_dotNet) _dotNet.invokeMethodAsync('OnMarkerClick', num);
        });

        _markers[order.orderNumber] = marker;

        // Nominatim rate limit: 1 req/sec
        await sleep(1100);
    }
};

window.mapDestroy = function () {
    Object.values(_markers).forEach(m => { try { m.remove(); } catch (e) { } });
    _markers = {};
    if (_map) { try { _map.remove(); } catch (e) { } _map = null; }
};

window.mapFlyTo = function (orderNumber) {
    const marker = _markers[orderNumber];
    if (_map && marker) {
        _map.flyTo(marker.getLatLng(), 16, { animate: true, duration: 0.8 });
        marker.openPopup();
    }
};

async function geocode(address) {
    try {
        const q = encodeURIComponent(address + ', Алматы, Казахстан');
        const url = `https://nominatim.openstreetmap.org/search?q=${q}&format=json&limit=1&accept-language=ru`;
        const r = await fetch(url, { headers: { 'User-Agent': 'WebOrdersApp/1.0' } });
        const d = await r.json();
        if (d && d.length > 0) {
            return [parseFloat(d[0].lat), parseFloat(d[0].lon)];
        }
    } catch (e) {
        console.warn('Geocode failed:', address, e);
    }
    return null;
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}
