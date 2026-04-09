let _map = null;
let _markers = [];
let _dotNet = null;

async function ensureMapGL() {
    if (window.mapgl) return;
    await new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = 'https://mapgl.2gis.com/api/js/v1';
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    });
}

window.mapInit = async function (orders, dotNetRef) {
    await ensureMapGL();
    _dotNet = dotNetRef;

    // Destroy old map
    _markers.forEach(m => { try { m.destroy(); } catch (e) { } });
    _markers = [];
    if (_map) { try { _map.destroy(); } catch (e) { } _map = null; }

    const container = document.getElementById('map-container');
    if (!container) return;

    _map = new mapgl.Map('map-container', {
        center: [76.9286, 43.2567],
        zoom: 12,
        key: 'demos-api-key'
    });

    for (const order of orders) {
        if (!order.address) continue;
        const coords = await geocodeAddress(order.address);
        if (!coords) continue;

        const marker = new mapgl.Marker(_map, {
            coordinates: coords
        });

        const idx = order.orderNumber;
        marker.on('click', () => {
            if (_dotNet) _dotNet.invokeMethodAsync('OnMarkerClick', idx);
        });

        _markers.push(marker);
    }
};

window.mapDestroy = function () {
    _markers.forEach(m => { try { m.destroy(); } catch (e) { } });
    _markers = [];
    if (_map) { try { _map.destroy(); } catch (e) { } _map = null; }
};

window.mapFlyTo = function (lon, lat) {
    if (_map) _map.setCenter([lon, lat], { animate: true });
    if (_map) _map.setZoom(15, { animate: true });
};

async function geocodeAddress(address) {
    try {
        const q = encodeURIComponent(address + ', Алматы');
        const url = `https://catalog.api.2gis.com/3.0/items/geocode?q=${q}&fields=items.point&key=demos-api-key`;
        const r = await fetch(url);
        const d = await r.json();
        if (d.result?.items?.[0]?.point) {
            const p = d.result.items[0].point;
            return [p.lon, p.lat];
        }
    } catch (e) {
        console.warn('Geocode failed:', address, e);
    }
    return null;
}
