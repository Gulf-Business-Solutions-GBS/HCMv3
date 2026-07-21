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

    getDefaultLogoUrl(skin?: string, small: boolean = false): string {
        const resolvedSkin = skin || this.skin;
        const suffix = small ? '-sm' : '';
        return `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${resolvedSkin}${suffix}.svg`;
    }

    notifyLogoChanged(skin: string): void {
        this._logoChanged$.next(skin);
    }
}
