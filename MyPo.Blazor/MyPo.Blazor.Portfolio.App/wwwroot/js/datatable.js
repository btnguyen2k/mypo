/* Datatable utilities */

export function MakeDatatable(elId) {
    const el = document.getElementById(elId);
    console.log("[DEBUG] MakeDatatable:", elId);
    console.log("[DEBUG] MakeDatatable:", el);
    if (!el) return;
    let table = new DataTable(elId, {
        // options
        info: true,
        pageLength: 20,
        lengthMenu: [10, 20, 50, 100],
    });
}
