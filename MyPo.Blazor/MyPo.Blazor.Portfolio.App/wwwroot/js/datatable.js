/* Datatable utilities */

function makeDatatableWithRetries(elId, tries = 0, retries = 3, delay = 100) {
    const elIdNoHash = elId.replace("#", "");
    const elIdHash = "#" + elIdNoHash;
    const el = document.getElementById(elIdNoHash);
    if (!el) return null;
    if (!window.jQuery && tries < retries) {
        console.error(`jQuery is not loaded. Retry in ${delay} ms.`);
        setTimeout(() => makeDatatableWithRetries(elId, tries + 1, retries, delay+50), delay);
        return null;
    }
    if (typeof DataTable === "undefined" && tries < retries) {
        console.error(`DataTable is not loaded. Retry in ${delay} ms.`);
        setTimeout(() => makeDatatableWithRetries(elId, tries + 1, retries, delay+50), delay);
        return null;
    }

    let table = new DataTable(elIdHash, {
        retrieve: true, // https://datatables.net/manual/tech-notes/3
        // options
        info: true,
        pageLength: 20,
        lengthMenu: [10, 20, 50, 100],
    });
    return table;
}

export function MakeDatatable(elId) {
    return makeDatatableWithRetries(elId, 0, 3, 100);
}
