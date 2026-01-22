/* Local storage utilities */

export function LocalStoreSet(key, val) {
    if (val === null || val === undefined) {
        localStorage.removeItem(key);
    } else {
        localStorage.setItem(key, val);
    }
}

export function LocalStoreGet(key) {
    return localStorage.getItem(key);
}

export function LocalStoreGetAsNumber(key) {
    return Number(localStorage.getItem(key));
}

export function LocalStoreSetJson(key, val) {
    if (val === null || val === undefined) {
        localStorage.removeItem(key);
    } else {
        localStorage.setItem(key, JSON.stringify(val));
    }
}

export function LocalStoreGetJson(key) {
    const item = localStorage.getItem(key);
    if (item === null || item === undefined) {
        return null;
    }
    return JSON.parse(item);
}
