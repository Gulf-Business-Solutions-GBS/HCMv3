import { Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '@core/auth.service';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { ToastService } from '@core/toast.service';
import { AuthShellComponent } from './auth-shell.component';

@Component({
    selector: 'careers-login',
    imports: [FormsModule, RouterLink, LocalizePipe, AuthShellComponent],
    templateUrl: './login.component.html',
})
export class LoginComponent {
    private _auth = inject(AuthService);
    private _router = inject(Router);
    private _route = inject(ActivatedRoute);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly emailAddress = signal('');
    readonly password = signal('');
    readonly rememberMe = signal(true);
    readonly submitting = signal(false);

    readonly title = this._localization.localize('CareersSignInTitle');
    readonly subtitle = this._localization.localize('CareersSignInSubtitle');

    async submit(form: NgForm): Promise<void> {
        if (form.invalid || this.submitting()) {
            return;
        }

        this.submitting.set(true);

        try {
            const result = await this._auth.login(
                this.emailAddress().trim(),
                this.password(),
                this.rememberMe()
            );

            if (result.success) {
                const returnUrl = this._route.snapshot.queryParamMap.get('returnUrl');
                // Only ever navigate to an in-app path: a returnUrl is attacker-controllable and an
                // absolute URL here would be an open redirect off a public login page.
                await this._router.navigateByUrl(
                    returnUrl && returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/'
                );
                return;
            }

            if (result.requiresTwoFactor) {
                this._toast.error(this._localization.localize('CareersTwoFactorNotSupported'));
                return;
            }

            if (result.requiresPasswordReset) {
                this._toast.error(this._localization.localize('CareersMustResetPassword'));
                return;
            }

            // The server's message (lockout, deactivated tenant, ...) is more useful than a generic one.
            this._toast.error(result.errorMessage ?? this._localization.localize('CareersLoginFailed'));
        } finally {
            this.submitting.set(false);
        }
    }
}
