/* Datatable utilities */

export function MakeDatatable(elId) {
    let table = new DataTable(elId, {
        // options
        info: true,
        pageLength: 20,
        lengthMenu: [10, 20, 50, 100]
    });
}
