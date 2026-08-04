import { Injectable, computed, signal } from '@angular/core';
import { AppConsts } from './app-consts';

export interface CareersLoginResult {
    success: boolean;
    /** Set when the tenant requires two-factor verification, which this portal does not implement. */
    requiresTwoFactor?: boolean;
    /** Set when the account must change its password before it can be used. */
    requiresPasswordReset?: boolean;
    errorMessage?: string;
}

/**
 * Candidate authentication.
 *
 * THERE IS NO CAREERS LOGIN ENDPOINT AND THERE MUST NOT BE ONE. Candidates are ordinary ABP users in the
 * `Candidate` role, so this posts to the STOCK `/api/TokenAuth/Authenticate`, which gives the portal
 * password hashing, failed-attempt lockout, two-factor and login-attempt auditing for free. What makes that
 * endpoint land on the right tenant is `HCMv3.Careers.CareersTenantResolveContributor` reading the careers
 * subdomain off the Host header -- which is also why `AppConsts.remoteServiceBaseUrl` is built from
 * `window.location.hostname` and not from a fixed host.
 *
 * This is deliberately NOT an NSwag-generated proxy call: TokenAuth is outside the careers service surface
 * and the portal's NSwag config only generates the three careers clients.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly _token = signal<string | null>(localStorage.getItem(AppConsts.authTokenName));

    readonly isAuthenticated = computed(() => !!this._token());

    get token(): string | null {
        return this._token();
    }

    async login(userNameOrEmailAddress: string, password: string, rememberClient: boolean): Promise<CareersLoginResult> {
        let response: Response;

        try {
            response = await fetch(AppConsts.remoteServiceBaseUrl + '/api/TokenAuth/Authenticate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    userNameOrEmailAddress,
                    password,
                    rememberClient,
                }),
            });
        } catch {
            return { success: false, errorMessage: null };
        }

        let body: any = null;
        try {
            body = await response.json();
        } catch {
            /* fall through to the generic failure below */
        }

        if (!response.ok || body?.success === false) {
            return {
                success: false,
                errorMessage: body?.error?.message ?? null,
            };
        }

        const result = body?.result;

        if (result?.requiresTwoFactorVerification) {
            return { success: false, requiresTwoFactor: true };
        }

        if (result?.shouldResetPassword) {
            return { success: false, requiresPasswordReset: true };
        }

        if (!result?.accessToken) {
            return { success: false };
        }

        localStorage.setItem(AppConsts.authTokenName, result.accessToken);
        if (result.encryptedAccessToken) {
            localStorage.setItem(AppConsts.encryptedAuthTokenName, result.encryptedAccessToken);
        }
        this._token.set(result.accessToken);

        return { success: true };
    }

    logout(): void {
        localStorage.removeItem(AppConsts.authTokenName);
        localStorage.removeItem(AppConsts.encryptedAuthTokenName);
        this._token.set(null);
    }
}
