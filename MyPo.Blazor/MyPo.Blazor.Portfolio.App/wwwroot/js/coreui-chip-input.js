/* CoreUI chip input helper */

export function InitChipInput(elId, separators = ",") {
    // const el = document.querySelector('.chip-input')
    const el = document.getElementById(elId);
    if (!el) {
        console.warn(`InitChipInput - Element with id '${elId}' not found.`);
        return;
    }
    const chipInput = coreui.ChipInput.getOrCreateInstance(el, {
        separator: separators,
    })
}
