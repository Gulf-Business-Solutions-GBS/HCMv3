import { Injectable, inject } from '@angular/core';
import { EssSessionServiceProxy } from '@shared/service-proxies/service-proxies';
import { PermissionCheckerService } from 'abp-ng2-module';

/**
 * Knows which self-service transactions this tenant has switched on, so the menu can hide the rest.
 *
 * <p>The home-page tiles have always come from the catalog, but the left-hand menu was written by hand.
 * Switching a type off therefore removed its tile and left its menu entry pointing at a screen that no
 * longer applied -- which reads as a broken link rather than a setting.</p>
 *
 * <p>The menu keeps its hand-authored grouping (General, Leave, Travel, Payroll) because the catalog has
 * no notion of groups and driving the whole menu from it would flatten twenty items into one list. The
 * catalog decides what is SHOWN; the menu decides where it sits.</p>
 */
@Injectable({ providedIn: 'root' })
export class EssMenuCatalogService {
    private _essSessionService = inject(EssSessionServiceProxy);
    private _permissionChecker = inject(PermissionCheckerService);

    /** Codes of the transactions this employee may actually open. Empty until the first load returns. */
    private _enabled = new Set<string>();

    /** Until this is true nothing is hidden -- see the reasoning on isEnabled. */
    private _loaded = false;

    private _loading = false;

    /**
     * Starts the one load, if it has not started already. Safe to call on every menu render.
     *
     * <p>A failure is swallowed deliberately. The menu is not the security boundary -- every self-service
     * endpoint is permission-checked and employee-scoped on the server -- so the worst case of not knowing
     * is an entry that should have been hidden, which is exactly where the product already was.</p>
     */
    ensureLoaded(): void {
        if (this._loaded || this._loading) {
            return;
        }

        // Somebody without self service -- the host, above all -- has no self-service menu to trim, and
        // asking anyway is refused by the server and put on screen as a "permissions are not granted"
        // dialog by the HTTP layer, however quietly the error is handled here.
        if (!this._permissionChecker.isGranted('Ess')) {
            this._loaded = true;
            return;
        }

        this._loading = true;

        this._essSessionService.getEssContext().subscribe({
            next: (context) => {
                this._enabled = new Set((context?.availableTransactionTypes ?? []).map((t) => t.code));
                this._loaded = true;
                this._loading = false;
            },
            error: () => {
                this._loading = false;
            },
        });
    }

    /**
     * Whether a menu entry governed by this catalog code should be shown.
     *
     * <p>True while the answer is still unknown. Showing an entry that should have been hidden is the
     * milder of the two mistakes: the other way round the menu would lose items on every page load and
     * put them back a moment later, which looks like a fault rather than a setting.</p>
     */
    isEnabled(transactionType: string): boolean {
        if (!this._loaded) {
            return true;
        }

        return this._enabled.has(transactionType);
    }

    /** Drops what was loaded, for a sign-out or after the catalog itself has been edited. */
    reset(): void {
        this._enabled = new Set<string>();
        this._loaded = false;
        this._loading = false;
    }
}
