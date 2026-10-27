import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppSessionService } from '@shared/common/session/app-session.service';

@Injectable({
    providedIn: 'root',
})
export class LogoService {
    private _dateTimeService = inject(DateTimeService);
    private _appSessionService = inject(AppSessionService);

    private remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    private _logoChanged$ = new Subject<string>();

    get logoChanged$() {
        return this._logoChanged$.asObservable();
    }

    private get skin(): string {
        return this._appSessionService.theme?.baseSettings?.layout?.darkMode ? 'dark' : 'light';
    }

    getLogoUrl(skin?: string, small: boolean = false): string {
        const resolvedSkin = skin || this.skin;
        const tenantId = this._appSessionService.tenant?.id;
        const date = this._dateTimeService.getDate().toDateTime();

        const suffix = small ? '-sm' : '';

        return `${this.remoteServiceBaseUrl}/TenantCustomization/GetTenantLogo?skin=${resolvedSkin}${suffix}&tenantId=${tenantId}&c=${date}`;
    }

    /**
     * The logo shown when there is no tenant, or the tenant has not uploaded one: GBS.
     *
     * PNG, NOT SVG. The supplied artwork is a raster; wrapping it in an SVG would have kept the old file
     * extension while changing nothing about what it is, and auto-tracing a 96x65 bitmap of a circular
     * mark produces a visibly worse logo than the bitmap. If vector artwork turns up, swap these four
     * files for .svg and change the extension here -- nothing else needs to know.
     */
    getDefaultLogoUrl(skin?: string, small: boolean = false): string {
        const resolvedSkin = skin || this.skin;
        const suffix = small ? '-sm' : '';
        return `${AppConsts.appBaseUrl}/assets/common/images/gbs-logo-on-${resolvedSkin}${suffix}.png`;
    }

    notifyLogoChanged(skin: string): void {
        this._logoChanged$.next(skin);
    }
}
