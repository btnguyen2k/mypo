// Chart.js interop helper for Blazor.
// Loads Chart.js (auto-registering build) lazily from CDN and manages chart instances by canvas id.

let chartCtorPromise = null;

async function getChartCtor() {
    if (!chartCtorPromise) {
        chartCtorPromise = import('https://cdn.jsdelivr.net/npm/chart.js@4.4.4/auto/+esm')
            .then(m => m.default ?? m.Chart);
    }
    return chartCtorPromise;
}

const instances = {};

export async function render(canvasId, config) {
    const ChartCtor = await getChartCtor();
    const el = document.getElementById(canvasId);
    if (!el) {
        return;
    }
    if (instances[canvasId]) {
        instances[canvasId].destroy();
        delete instances[canvasId];
    }
    instances[canvasId] = new ChartCtor(el, config);
}

export function destroy(canvasId) {
    if (instances[canvasId]) {
        instances[canvasId].destroy();
        delete instances[canvasId];
    }
}
