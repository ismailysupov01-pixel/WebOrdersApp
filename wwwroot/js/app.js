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

// ===== SCROLL PRESERVATION =====
window.getScrollTop = function (selector) {
    const el = document.querySelector(selector);
    return el ? el.scrollTop : 0;
};
window.setScrollTop = function (selector, pos) {
    const el = document.querySelector(selector);
    if (el) el.scrollTop = pos;
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

    // Форматирование: +7XXX XXX XX XX
    function applyFormat(digits10) {
        let r = '+7';
        if (digits10.length > 0) r += digits10.slice(0, 3);
        if (digits10.length > 3) r += ' ' + digits10.slice(3, 6);
        if (digits10.length > 6) r += ' ' + digits10.slice(6, 8);
        if (digits10.length > 8) r += ' ' + digits10.slice(8, 10);
        return r;
    }

    // Извлечь 10 цифр из строки (обрезаем лишний префикс 7/8 если 11 цифр)
    function extractDigits(raw) {
        let d = raw.replace(/\D/g, '');
        if (d.length === 11 && (d.startsWith('7') || d.startsWith('8'))) d = d.slice(1);
        return d.slice(0, 10);
    }

    function handleInput(e) {
        const input = e.target;
        let value = input.value;
        if (!value.startsWith('+7')) value = '+7';
        const digits = value.slice(2).replace(/\D/g, '').slice(0, 10);
        input.value = applyFormat(digits);
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

    // Вставка из буфера: +7XXXXXXXXXX → +7XXX XXX XX XX
    function handlePaste(e) {
        e.preventDefault();
        const raw = (e.clipboardData || window.clipboardData).getData('text');
        const digits = extractDigits(raw);
        e.target.value = applyFormat(digits);
        e.target.setSelectionRange(e.target.value.length, e.target.value.length);
        // Уведомляем Blazor об изменении значения
        e.target.dispatchEvent(new Event('input', { bubbles: true }));
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

    // Начальное значение — если уже что-то есть, форматируем
    if (!el.value) {
        el.value = '+7';
    } else {
        const digits = extractDigits(el.value);
        el.value = applyFormat(digits) || '+7';
    }
};
