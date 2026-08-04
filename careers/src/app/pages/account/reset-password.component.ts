import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DateTime } from 'luxon';
import { firstValueFrom } from 'rxjs';

import { CareersAccountServiceProxy, CareersResetPasswordInput } from '@proxies/service-proxies';
import { AuthService } from '@core/auth.service';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { RecaptchaService } from '@core/recaptcha.service';
import { ToastService } from '@core/toast.service';
import { AuthShellComponent } from './auth-shell.component';

/**
 * Lands the link mailed by `CareersAccountAppService.SendPasswordResetCode`.
 *
 * The query-string parameter names are fixed by `CareersUrlService.CreatePasswordResetUrlFormatOrNull`:
 * `?userId=...&resetCode=...&expireDate=...&tenantId=...`. The route segment (`account/reset-password`)
 * matches `CareersUrlService.PasswordResetRoute`.
 */
@Component({
    selector: 'careers-reset-password',
    imports: [FormsModule, RouterLink, LocalizePipe, AuthShellComponent],
    templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent {
    private _accountService = inject(CareersAccountServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _recaptcha = inject(RecaptchaService);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);
    private _auth = inject(AuthService);

    private readonly _params = this._route.snapshot.queryParamMap;

    readonly userId = Number(this._params.get('userId'));
    readonly resetCode = this._params.get('resetCode') ?? '';
    readonly expireDateRaw = this._params.get('expireDate') ?? '';

    readonly password = signal('');
    readonly confirmPassword = signal('');
    readonly submitting = signal(false);
    readonly succeeded = signal(false);

    readonly title = this._localization.localize('CareersResetPasswordTitle');

    readonly linkIsValid = computed(() => !!this.userId && !!this.resetCode);

    readonly passwordsMatch = computed(
        () => !this.confirmPassword() || this.password() === this.confirmPassword()
    );

    async submit(form: NgForm): Promise<void> {
        if (form.invalid || !this.passwordsMatch() || !this.linkIsValid() || this.submitting()) {
            return;
        }

        this.submitting.set(true);

        try {
            const captchaResponse = this._recaptcha.isRequiredForPasswordReset
                ? await this._recaptcha.execute('careers_reset_password')
                : undefined;

            // `expireDate` is a required Luxon DateTime on the generated DTO. It is echoed back exactly as
            // the emailed link carried it; the server re-checks expiry itself and rejects a stale code.
            const parsedExpireDate = this.expireDateRaw ? DateTime.fromISO(this.expireDateRaw) : null;

            const input = new CareersResetPasswordInput({
                userId: this.userId,
                resetCode: this.resetCode,
                password: this.password(),
                captchaResponse,
                expireDate: parsedExpireDate?.isValid ? parsedExpireDate : (undefined as any),
            });

            const result = await firstValueFrom(this._accountService.resetPassword(input));

            this.succeeded.set(true);

            // A password change invalidates any token this browser was holding.
            this._auth.logout();

            if (result.canLogin) {
                this._toast.success(this._localization.localize('CareersPasswordResetSucceeded'));
            }
        } catch (error) {
            this._toast.apiError(error);
        } finally {
            this.submitting.set(false);
        }
    }

    goToLogin(): void {
        void this._router.navigate(['/account/login']);
    }
}
