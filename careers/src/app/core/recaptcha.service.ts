import { Injectable } from '@angular/core';
import { AppConsts } from './app-consts';

declare const grecaptcha: any;

/**
 * Google reCAPTCHA v3, loaded on demand.
 *
 * `CareersAccountAppService` validates `CaptchaResponse` whenever the tenant has
 * `App.UserManagement.UseCaptchaOnRegistration` / `...UseCaptchaOnResetPassword` enabled -- and BOTH default
 * to `true`, so registration and password reset will be rejected without a token on a stock tenant.
 *
 * Implemented directly against the Google script rather than pulling in `ng-recaptcha-2` (which the admin
 * SPA uses): v3 is a two-call API with no UI, and a dependency on a public page has to earn its bytes.
 * The script is injected only when a captcha is actually required, so a visitor who only browses jobs never
 * loads a third-party script.
 */
@Injectable({ providedIn: 'root' })
export class RecaptchaService {
    private _scriptPromise: Promise<void> | null = null;

    /** True when the tenant requires a captcha for candidate self-registration. */
    get isRequiredForRegistration(): boolean {
        return this.isEnabled(AppConsts.settingNames.useCaptchaOnRegistration);
    }

    /** True when the tenant requires a captcha for password reset. */
    get isRequiredForPasswordReset(): boolean {
        return this.isEnabled(AppConsts.settingNames.useCaptchaOnResetPassword);
    }

    private isEnabled(settingName: string): boolean {
        // No site key means reCAPTCHA is not configured at all; sending an empty token would just fail
        // server-side with a less useful message than the real one.
        return !!AppConsts.recaptchaSiteKey && AppConsts.settings[settingName] === 'true';
    }

    /**
     * Returns a fresh v3 token for `action`, or `undefined` when captcha is not configured. Never throws:
     * a captcha outage should surface as the server's own validation error, not as an unhandled rejection
     * that leaves the submit button spinning.
     */
    async execute(action: string): Promise<string | undefined> {
        if (!AppConsts.recaptchaSiteKey) {
            return undefined;
        }

        try {
            await this.loadScript();
            return await new Promise<string>((resolve, reject) => {
                grecaptcha.ready(() => {
                    grecaptcha
                        .execute(AppConsts.recaptchaSiteKey, { action })
                        .then(resolve)
                        .catch(reject);
                });
            });
        } catch {
            return undefined;
        }
    }

    private loadScript(): Promise<void> {
        this._scriptPromise ??= new Promise<void>((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `https://www.google.com/recaptcha/api.js?render=${AppConsts.recaptchaSiteKey}`;
            script.async = true;
            script.defer = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Could not load reCAPTCHA.'));
            document.head.appendChild(script);
        });

        return this._scriptPromise;
    }
}
