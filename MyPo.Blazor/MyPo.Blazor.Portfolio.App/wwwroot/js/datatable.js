/* Datatable utilities */

export function MakeDatatable(elId) {
    const elIdNoHash = elId.replace("#", "");
    const elIdHash = "#" + elIdNoHash;
    const el = document.getElementById(elIdNoHash);
    if (!el) return;
    let table = new DataTable(elIdHash, {
        // options
        info: true,
        pageLength: 20,
        lengthMenu: [10, 20, 50, 100],
    });
}
