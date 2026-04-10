// ===== SESSION (localStorage) =====
window.saveSession = function (data) {
    localStorage.setItem('weborders_v2', JSON.stringify(data));
};
window.loadSession = function () {
    try {
        const s = localStorage.getItem('weborders_v2');
        return s ? JSON.parse(s) : null;
    } catch (e) { return null; }
};
window.clearSession = function () {
    localStorage.removeItem('weborders_v2');
};

// ===== BROWSER NOTIFICATIONS =====
window.requestNotifyPermission = async function () {
    if (!('Notification' in window)) return false;
    const p = await Notification.requestPermission();
    return p === 'granted';
};
window.showNotify = function (title, body) {
    if ('Notification' in window && Notification.permission === 'granted') {
        new Notification(title, { body: body });
    }
};

// ===== MAP HELPERS =====
window.mapInvalidateSize = function () {
    if (typeof _map !== 'undefined' && _map) {
        setTimeout(() => { try { _map.invalidateSize(); } catch (e) { } }, 200);
    }
};

// ===== PHONE MASK =====
window.initPhoneMask = function (id) {
    const el = document.getElementById(id);
    if (!el) return;

    el.addEventListener('focus', () => {
        if (!el.value || el.value === '') el.value = '+7 ';
        setTimeout(() => el.setSelectionRange(el.value.length, el.value.length), 0);
    });

    el.addEventListener('keydown', (e) => {
        const pos = el.selectionStart;
        // Protect prefix "+7 "
        if (pos <= 3 && (e.key === 'Backspace' || e.key === 'Delete')) {
            e.preventDefault();
        }
    });

    el.addEventListener('input', () => {
        let raw = el.value;
        let digits = raw.replace(/\D/g, '');
        // Remove leading 7 or 8
        if (digits.startsWith('8') || digits.startsWith('7')) digits = digits.slice(1);
        digits = digits.slice(0, 10);

        let result = '+7';
        if (digits.length > 0) result += ' ' + digits.slice(0, 3);
        if (digits.length > 3) result += ' ' + digits.slice(3, 6);
        if (digits.length > 6) result += ' ' + digits.slice(6, 8);
        if (digits.length > 8) result += ' ' + digits.slice(8, 10);

        el.value = result;
    });

    el.addEventListener('paste', (e) => {
        e.preventDefault();
        const pasted = (e.clipboardData || window.clipboardData).getData('text');
        let digits = pasted.replace(/\D/g, '');
        if (digits.startsWith('8') || digits.startsWith('7')) digits = digits.slice(1);
        digits = digits.slice(0, 10);
        let result = '+7';
        if (digits.length > 0) result += ' ' + digits.slice(0, 3);
        if (digits.length > 3) result += ' ' + digits.slice(3, 6);
        if (digits.length > 6) result += ' ' + digits.slice(6, 8);
        if (digits.length > 8) result += ' ' + digits.slice(8, 10);
        el.value = result;
    });
};
