export function InitDatetimePickers(opts = {}) {
    const elements = document.querySelectorAll('[data-datetime-picker="true"]');
    for (let i = 0; i < elements.length; i++) {
        initDatetimePicker(elements[i], opts);
    }
}

function initDatetimePicker(el, opts = {}) {
    opts = opts ?? {};
    opts['localization'] = opts['localization'] ?? {format: 'dd-MMM-yyyy HH:mm'};
    opts['display'] = opts['display'] ?? {};
    opts['display']['icons'] = opts['display']['icons'] ?? {
        type: 'icons',
        time: 'bi bi-clock',
        date: 'bi bi-calendar-date',
        up: 'bi bi-arrow-up-circle',
        down: 'bi bi-arrow-down-circle',
        previous: 'bi bi-chevron-double-left',
        next: 'bi bi-chevron-double-right',
        today: 'bi bi-calendar-check',
        clear: 'bi bi-trash',
        close: 'bi bi-x'
    };
    opts['display']['buttons'] = opts['display']['buttons'] ?? {today: true, clear: false, close: true};
    const picker = new tempusDominus.TempusDominus(el, opts);
    return picker;
}
