import { Component, OnInit, ViewEncapsulation, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmailSettingsEditDto,
    HostBillingSettingsEditDto,
    InstallDto,
    InstallServiceProxy,
    NameValue,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';

import { ValidationMessagesComponent } from '../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../shared/utils/button-busy.directive';
import { EqualValidator } from '@shared/utils/validation/equal-validator.directive';
import { AppConsts } from '@shared/AppConsts';

@Component({
    templateUrl: './install.component.html',
    animations: [appModuleAnimation()],
    styleUrls: ['./install.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [FormsModule, ValidationMessagesComponent, ButtonBusyDirective, EqualValidator],
})
export class InstallComponent extends AppComponentBase implements OnInit {
    private _installSettingService = inject(InstallServiceProxy);

    readonly saving = signal(false);
    setupSettings: InstallDto;
    languages: NameValue[];
    tenantAdminPasswordRepeat = '';
    defaultLogo = AppConsts.appBaseUrl + '/assets/common/images/app-logo-on-light.svg';

    loadAppSettingsJson(): void {
        let self = this;
        self._installSettingService.getAppSettingsJson().subscribe((result) => {
            this.setupSettings.webSiteUrl = result.webSiteUrl;
            this.setupSettings.serverUrl = result.serverSiteUrl;
            this.languages = result.languages;
        });
    }

    init(): void {
        this._installSettingService.checkDatabase().subscribe((result) => {
            if (result.isDatabaseExist) {
                window.location.href = '/';
            }
        });

        this.setupSettings = new InstallDto();
        this.setupSettings.smtpSettings = new EmailSettingsEditDto();
        this.setupSettings.billInfo = new HostBillingSettingsEditDto();
        this.setupSettings.defaultLanguage = 'en';
        this.loadAppSettingsJson();
    }

    ngOnInit(): void {
        let self = this;
        self.init();
    }

    saveAll(): void {
        this.saving.set(true);
        this._installSettingService
            .setup(this.setupSettings)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                window.location.href = '/';
            });
    }

    goToHome(): void {
        (window as any).location.href = '/';
    }
}
