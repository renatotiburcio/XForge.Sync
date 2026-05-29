/**
 * XForge.Sync.IndexedDB — JS interop module for IndexedDB operations.
 * Used by IndexedDbLocalStorage to provide offline-first browser storage.
 *
 * Functions are registered on window.xForgeIndexedDb for global access via IJSRuntime.
 */

function openDatabase(dbName, version, itemsStoreName, metadataStoreName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, version);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;

            if (!db.objectStoreNames.contains(itemsStoreName)) {
                db.createObjectStore(itemsStoreName, { keyPath: 'key' });
            }

            if (!db.objectStoreNames.contains(metadataStoreName)) {
                db.createObjectStore(metadataStoreName, { keyPath: 'key' });
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(new Error('Failed to open database: ' + (request.error ? request.error.message : 'unknown')));
    });
}

function getItem(db, storeName, key) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.get(key);

        request.onsuccess = () => resolve(request.result || null);
        request.onerror = () => reject(new Error('Failed to get item: ' + (request.error ? request.error.message : 'unknown')));
    });
}

function setItem(db, storeName, value) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.put(value);

        request.onsuccess = () => resolve();
        request.onerror = () => reject(new Error('Failed to set item: ' + (request.error ? request.error.message : 'unknown')));
    });
}

function deleteItem(db, storeName, key) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.delete(key);

        request.onsuccess = () => resolve();
        request.onerror = () => reject(new Error('Failed to delete item: ' + (request.error ? request.error.message : 'unknown')));
    });
}

function getAllItems(db, storeName) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.getAll();

        request.onsuccess = () => resolve(request.result || []);
        request.onerror = () => reject(new Error('Failed to get all items: ' + (request.error ? request.error.message : 'unknown')));
    });
}

function closeDatabase(db) {
    if (db && typeof db.close === 'function') {
        db.close();
    }
}

// Register on global namespace for IJSRuntime access
window.xForgeIndexedDb = {
    openDatabase,
    getItem,
    setItem,
    deleteItem,
    getAllItems,
    closeDatabase
};
