// Keyboard capture and localStorage bridge for the MB-12c.
window.mb12c = {
    _handledKeys: new Set([
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        '.', ',', '+', '-', '*', '/', '%', 'Enter', 'Backspace', 'Delete', 'Escape',
        ' ', 'ArrowDown', 'ArrowUp',
        'f', 'g', 's', 'r', 'n', 'i', 'p', 'm', 'v', 'c', 'e', 'y', 'x', 'd', 'q', 'l',
        'F', 'G', 'S', 'R', 'N', 'I', 'P', 'M', 'V', 'C', 'E', 'Y', 'X', 'D', 'Q', 'L',
    ]),

    init: function (dotnetRef) {
        window.addEventListener('keydown', function (e) {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            if (!window.mb12c._handledKeys.has(e.key)) return;
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnBrowserKey', e.key);
        });
    },

    save: function (key, value) { try { localStorage.setItem(key, value); } catch { } },
    load: function (key) { try { return localStorage.getItem(key); } catch { return null; } },
};
