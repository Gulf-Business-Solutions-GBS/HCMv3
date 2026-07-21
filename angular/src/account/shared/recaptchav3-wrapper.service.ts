import { Injectable, inject } from '@angular/core';
import { SettingService } from 'abp-ng2-module';
import { ReCaptchaV3Service } from 'ng-recaptcha-2';
import { AbpSessionService } from 'abp-ng2-module';

@Injectable()
export class ReCaptchaV3WrapperService {
    private setting = inject(SettingService);
    private _reCaptchaV3Service = inject(ReCaptchaV3Service);
    private _sessionService = inject(AbpSessionService);

    private captchaSettings: any;

    constructor() {
        this.initializeCaptchaSettings();
    }

    getService(): ReCaptchaV3Service {
        return this._reCaptchaV3Service;
    }

    useCaptchaOnLogin(): boolean {
        return this.useCaptcha(this.captchaSettings.login);
    }

    setCaptchaVisibilityOnLogin(): void {
        this.setCaptchaVisibility(this.captchaSettings.login);
    }

    useCaptchaOnRegister(): boolean {
        return this.useCaptcha(this.captchaSettings.register);
    }

    setCaptchaVisibilityOnRegister(): void {
        this.setCaptchaVisibility(this.captchaSettings.register);
    }

    useCaptchaOnResetPassword(): boolean {
        return this.useCaptcha(this.captchaSettings.resetPassword);
    }

    setCaptchaVisibilityOnResetPassword(): void {
        this.setCaptchaVisibility(this.captchaSettings.resetPassword);
    }

    useCaptchaOnEmailActivation(): boolean {
        return this.useCaptcha(this.captchaSettings.emailActivation);
    }

    setCaptchaVisibilityOnEmailActivation(): void {
        this.setCaptchaVisibility(this.captchaSettings.emailActivation);
    }

    private initializeCaptchaSettings(): void {
        const hasTenant = this._sessionService.tenantId > 0;
        this.captchaSettings = {
            login: 'App.UserManagement.UseCaptchaOnLogin',
            register: hasTenant
                ? 'App.UserManagement.UseCaptchaOnRegistration'
                : 'App.TenantManagement.UseCaptchaOnRegistration',
            resetPassword: hasTenant
                ? 'App.UserManagement.UseCaptchaOnResetPassword'
                : 'App.TenantManagement.UseCaptchaOnResetPassword',
            emailActivation: hasTenant
                ? 'App.UserManagement.UseCaptchaOnEmailActivation'
                : 'App.TenantManagement.UseCaptchaOnEmailActivation',
        };
    }

    private useCaptcha(settingKey: string): boolean {
        return this.setting.getBoolean(settingKey);
    }

    private setCaptchaVisibility(settingKey: string): void {
        const recaptchaElements = document.getElementsByClassName('grecaptcha-badge');

        if (recaptchaElements.length <= 0) {
            const observer = new MutationObserver((mutations, obs) => {
                const elements = document.getElementsByClassName('grecaptcha-badge');
                if (elements.length > 0) {
                    this.toggleCaptchaVisibility(elements[0], settingKey);
                    obs.disconnect();
                }
            });
            observer.observe(document.body, {
                childList: true,
                subtree: true,
            });
            return;
        }

        this.toggleCaptchaVisibility(recaptchaElements[0], settingKey);
    }

    private toggleCaptchaVisibility(element: Element, settingKey: string): void {
        if (this.useCaptcha(settingKey)) {
            element.classList.remove('d-none');
        } else {
            element.classList.add('d-none');
        }
    }
}
