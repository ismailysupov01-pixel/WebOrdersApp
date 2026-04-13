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
    if (!el || el.dataset.maskInit) return;
    el.dataset.maskInit = '1';

    // Всегда начинаем с +7, только цифры, максимум 10 цифр после +7
    function handleInput(e) {
        const input = e.target;
        let value = input.value;
        if (!value.startsWith('+7')) value = '+7';
        let digits = value.slice(2).replace(/\D/g, '').slice(0, 10);
        input.value = '+7' + digits;
        input.setSelectionRange(input.value.length, input.value.length);
    }

    // Запрет ввода любых символов кроме цифр
    function handleKeyDown(e) {
        const allowedKeys = [46, 8, 9, 27, 13]; // delete, backspace, tab, escape, enter
        const ctrlKeys = [65, 67, 86, 88];       // Ctrl+A, C, V, X
        if (
            allowedKeys.includes(e.keyCode) ||
            (ctrlKeys.includes(e.keyCode) && e.ctrlKey) ||
            (e.keyCode >= 35 && e.keyCode <= 39)  // Home, End, стрелки
        ) return;
        if ((e.keyCode < 48 || e.keyCode > 57) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    }

    // Вставка из буфера — берём последние 10 цифр
    function handlePaste(e) {
        e.preventDefault();
        let pasted = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '');
        pasted = pasted.slice(-10);
        e.target.value = '+7' + pasted;
        e.target.setSelectionRange(e.target.value.length, e.target.value.length);
    }

    // Фокус — устанавливаем +7, курсор в конец
    function handleFocus(e) {
        const input = e.target;
        if (!input.value || input.value === '+') input.value = '+7';
        input.setSelectionRange(input.value.length, input.value.length);
    }

    el.addEventListener('input',   handleInput);
    el.addEventListener('keydown', handleKeyDown);
    el.addEventListener('paste',   handlePaste);
    el.addEventListener('focus',   handleFocus);

    // Начальное значение
    if (!el.value) el.value = '+7';
};
