/* Print support utilities */

function PrintMarkdownContent(elId, title) {
    const el = document.getElementById(elId);
    if (!el) {
        alert(`PrintMarkdownContent - Element with id '${elId}' not found.`);
        return;
    }
    const content = el.innerHTML;
    const html = `<html lang="en"><head><title>${title}</title>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css" rel="stylesheet">
        <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/katex.min.css">
        <style>body { padding: 2rem; } @media print { body { padding: 0; } }</style>
        </head><body><h1 class="mb-4">${title}</h1>\n${content}</body></html>`;
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const printWindow = window.open(url, '_blank');
    printWindow.onload = function() {
        printWindow.print();
        URL.revokeObjectURL(url);
    };
}
