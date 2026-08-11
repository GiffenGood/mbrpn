// Keyboard capture, viewport fitting and localStorage bridge for the MB-RPN.
window.mbrpn = {
    isTouch: function () {
        return window.matchMedia('(pointer: coarse)').matches
            || window.location.search.indexOf('mobile=1') >= 0;
    },

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
            if (!window.mbrpn._handledKeys.has(e.key)) return;
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnBrowserKey', e.key);
        });
        requestAnimationFrame(window.mbrpn.fit);
    },

    // Scales the calculator to the viewport. On touch devices in landscape it fills the
    // screen edge-to-edge like a real calculator; elsewhere it only ever shrinks to fit.
    fit: function () {
        const touch = window.mbrpn.isTouch();
        document.documentElement.classList.toggle('touch', touch);
        const el = document.querySelector('.calc-body');
        if (!el) return;
        const w = el.offsetWidth, h = el.offsetHeight;   // layout size, unaffected by transform
        if (!w || !h) return;
        const vw = window.innerWidth, vh = window.innerHeight;
        const landscape = vw > vh;
        const scale = (touch && landscape)
            ? Math.min(vw / w, vh / h)
            : Math.min(1, (vw - 24) / w, (vh - 24) / h);
        const prev = window.mbrpn._lastFit;
        if (prev && prev.w === w && prev.h === h && Math.abs(prev.scale - scale) < 0.001) return;
        window.mbrpn._lastFit = { w: w, h: h, scale: scale };
        const root = document.documentElement.style;
        root.setProperty('--calc-w', w + 'px');
        root.setProperty('--calc-h', h + 'px');
        root.setProperty('--calc-scale', scale);
    },

    save: function (key, value) { try { localStorage.setItem(key, value); } catch { } },
    load: function (key) { try { return localStorage.getItem(key); } catch { return null; } },
};

window.addEventListener('resize', function () { window.mbrpn.fit(); });
window.addEventListener('orientationchange', function () { setTimeout(window.mbrpn.fit, 60); });
if (window.ResizeObserver) {
    // Catches viewport/layout changes that fire no resize event (and Blazor's first render).
    new ResizeObserver(function () { window.mbrpn.fit(); }).observe(document.body);
}
window.mbrpn.fit();
