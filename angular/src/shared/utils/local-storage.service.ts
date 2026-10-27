import { Injectable } from '@angular/core';
import localforage from 'localforage';

@Injectable({ providedIn: 'root' })
export class LocalStorageService {
    getItem(key: string, callback: any): void {
        if (!localforage) {
            return;
        }

        localforage.getItem(key, callback);
    }

    setItem(key, value, callback?: any): void {
        if (!localforage) {
            return;
        }

        if (value === null) {
            value = undefined;
        }

        localforage.setItem(key, value, callback);
    }

    removeItem(key, callback?: any): void {
        if (!localforage) {
            return;
        }

        localforage.removeItem(key, callback);
    }
}
