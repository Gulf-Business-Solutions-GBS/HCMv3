import { Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CareersAccountServiceProxy, CareersSendPasswordResetCodeInput } from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { RecaptchaService } from '@core/recaptcha.service';
import { ToastService } from '@core/toast.service';
import { AuthShellComponent } from './auth-shell.component';

@Component({
    selector: 'careers-forgot-password',
    imports: [FormsModule, RouterLink, LocalizePipe, AuthShellComponent],
    templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
    private _accountService = inject(CareersAccountServiceProxy);
    private _recaptcha = inject(RecaptchaService);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly emailAddress = signal('');
    readonly submitting = signal(false);
    readonly sent = signal(false);

    readonly title = this._localization.localize('CareersForgotPasswordTitle');
    readonly subtitle = this._localization.localize('CareersForgotPasswordSubtitle');

    async submit(form: NgForm): Promise<void> {
        if (form.invalid || this.submitting()) {
            return;
        }

        this.submitting.set(true);

        try {
            const captchaResponse = this._recaptcha.isRequiredForPasswordReset
                ? await this._recaptcha.execute('careers_forgot_password')
                : undefined;

            await firstValueFrom(
                this._accountService.sendPasswordResetCode(
                    new CareersSendPasswordResetCodeInput({
                        emailAddress: this.emailAddress().trim(),
                        captchaResponse,
                    })
                )
            );

            // The endpoint returns void and behaves identically whether or not the address exists, so the
            // UI must show the same neutral confirmation either way -- echoing "no such account" here would
            // hand back the account-enumeration oracle the backend deliberately withholds.
            this.sent.set(true);
        } catch (error) {
            this._toast.apiError(error);
        } finally {
            this.submitting.set(false);
        }
    }
}
