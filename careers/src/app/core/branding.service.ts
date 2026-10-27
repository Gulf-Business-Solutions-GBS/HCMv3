import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CareersServiceProxy, PublicTenantBrandingDto } from '@proxies/service-proxies';
import { AppConsts } from './app-consts';

/**
 * Per-tenant branding for every page of the portal.
 *
 * `GetTenantBranding` deliberately returns NO binary ids -- only `HasLightLogo` / `HasDarkLogo` flags -- so
 * the logo itself is fetched from the already-anonymous
 * `/TenantCustomization/GetLogo?tenantId={id}` endpoint. That keeps `BinaryObject` Guids off the public
 * surface (see `PublicTenantBrandingDto`'s remarks).
 *
 * Loaded once, lazily, and cached in a signal: the shell renders the tenant name and logo on every route,
 * and re-fetching it per navigation would be a wasted round trip on a public site.
 */
@Injectable({ providedIn: 'root' })
export class BrandingService {
    private _careersService = inject(CareersServiceProxy);

    readonly branding = signal<PublicTenantBrandingDto | null>(null);
    readonly loadFailed = signal<boolean>(false);

    /** The tenant's display name, falling back to a neutral title before branding has loaded. */
    readonly displayName = computed(() => this.branding()?.name ?? '');

    readonly logoUrl = computed(() => {
        const branding = this.branding();
        if (!branding?.hasLightLogo) {
            return null;
        }

        return `${AppConsts.remoteServiceBaseUrl}/TenantCustomization/GetLogo?tenantId=${branding.tenantId}`;
    });

    private _loading: Promise<void> | null = null;

    /** Idempotent: concurrent callers during the initial render share one request. */
    load(): Promise<void> {
        if (this.branding() || this.loadFailed()) {
            return Promise.resolve();
        }

        this._loading ??= firstValueFrom(this._careersService.getTenantBranding())
            .then((result) => {
                this.branding.set(result);
            })
            .catch(() => {
                // Most likely the host name does not match App:CareersDomainFormat, in which case the
                // backend fails closed. The shell still renders; the page-level calls report the reason.
                this.loadFailed.set(true);
            });

        return this._loading;
    }
}
