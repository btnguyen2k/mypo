/* Datatable utilities */

export function MakeDatatable(elId) {
    const elIdNoHash = elId.replace("#", "");
    const elIdHash = "#" + elIdNoHash;
    const el = document.getElementById(elIdNoHash);
    if (!el) return null;
    let table = new DataTable(elIdHash, {
        retrieve: true, // https://datatables.net/manual/tech-notes/3
        // options
        info: true,
        pageLength: 20,
        lengthMenu: [10, 20, 50, 100],
    });
    return table;
}
