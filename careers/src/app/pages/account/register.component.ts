import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CareersAccountServiceProxy, CareersRegisterInput } from '@proxies/service-proxies';
import { AppConsts } from '@core/app-consts';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { RecaptchaService } from '@core/recaptcha.service';
import { ToastService } from '@core/toast.service';
import { AuthShellComponent } from './auth-shell.component';

@Component({
    selector: 'careers-register',
    imports: [FormsModule, RouterLink, LocalizePipe, AuthShellComponent],
    templateUrl: './register.component.html',
})
export class RegisterComponent {
    private _accountService = inject(CareersAccountServiceProxy);
    private _recaptcha = inject(RecaptchaService);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly emailAddress = signal('');
    readonly firstNameEn = signal('');
    readonly lastNameEn = signal('');
    readonly firstNameAr = signal('');
    readonly lastNameAr = signal('');
    readonly password = signal('');
    readonly confirmPassword = signal('');

    readonly submitting = signal(false);
    /** Set once the account exists; the form is replaced by the "what happens next" message. */
    readonly succeeded = signal(false);
    readonly canLoginNow = signal(false);

    readonly title = this._localization.localize('CareersRegisterTitle');
    readonly subtitle = this._localization.localize('CareersRegisterSubtitle');

    /**
     * `App.Careers.AllowCandidateSelfRegistration` DEFAULTS TO FALSE, and the backend throws
     * `CareersRegistrationIsDisabled` when it is off. Reading the same client-visible setting here means a
     * candidate is told up front instead of after filling in the whole form.
     */
    readonly registrationEnabled = computed(
        () => AppConsts.settings[AppConsts.settingNames.allowCandidateSelfRegistration] === 'true'
    );

    readonly passwordsMatch = computed(
        () => !this.confirmPassword() || this.password() === this.confirmPassword()
    );

    async submit(form: NgForm): Promise<void> {
        if (form.invalid || !this.passwordsMatch() || this.submitting()) {
            return;
        }

        this.submitting.set(true);

        try {
            const captchaResponse = this._recaptcha.isRequiredForRegistration
                ? await this._recaptcha.execute('careers_register')
                : undefined;

            const input = new CareersRegisterInput({
                emailAddress: this.emailAddress().trim(),
                firstNameEn: this.firstNameEn().trim(),
                lastNameEn: this.lastNameEn().trim(),
                firstNameAr: this.firstNameAr().trim(),
                lastNameAr: this.lastNameAr().trim(),
                password: this.password(),
                captchaResponse,
            });

            const result = await firstValueFrom(this._accountService.register(input));

            this.canLoginNow.set(!!result.canLogin);
            this.succeeded.set(true);
        } catch (error) {
            this._toast.apiError(error);
        } finally {
            this.submitting.set(false);
        }
    }
}
