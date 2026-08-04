import { environment } from '@env/environment';
import { AppConsts } from './app-consts';

/**
 * Everything that must be true before Angular bootstraps.
 *
 * 1. Resolve the API base URL from the CURRENT HOST. The careers API resolves its tenant from the Host
 *    header (`App:CareersDomainFormat`), so the portal must call the API on the same subdomain the browser
 *    is on -- see `environments/environment.ts`.
 * 2. Download the ABP user configuration (`/AbpUserConfiguration/GetAll`), which is anonymous and yields
 *    both the `HCMv3` localization dictionary and the client-visible tenant settings (reCAPTCHA site key,
 *    whether captcha is required, whether candidate self-registration is enabled). Because
 *    `CareersTenantResolveContributor` makes ABP's ambient tenant resolution understand careers subdomains,
 *    this call is already scoped to the right tenant.
 *
 * Failure is NOT fatal: the portal still boots and renders, and the individual API calls surface the real
 * error. A blank white screen because a settings fetch failed would be a worse public experience than a
 * page that says what went wrong.
 */
export class CareersPreBootstrap {
    static async run(): Promise<void> {
        const host = window.location.hostname;

        AppConsts.remoteServiceBaseUrl = environment.remoteServiceBaseUrlFormat.replace('{HOST}', host);
        AppConsts.appBaseUrl = environment.appBaseUrlFormat.replace('{HOST}', host);

        await CareersPreBootstrap.loadAbpUserConfiguration();
    }

    private static async loadAbpUserConfiguration(): Promise<void> {
        const url = AppConsts.remoteServiceBaseUrl + '/AbpUserConfiguration/GetAll';

        const headers: Record<string, string> = { 'Content-Type': 'application/json' };
        const token = localStorage.getItem(AppConsts.authTokenName);
        if (token) {
            headers['Authorization'] = 'Bearer ' + token;
        }

        try {
            const response = await fetch(url, { method: 'GET', headers });
            if (!response.ok) {
                console.warn('Careers portal: could not load ABP user configuration.', response.status);
                return;
            }

            const body = await response.json();
            const result = body?.result;
            if (!result) {
                return;
            }

            AppConsts.localization = result.localization?.values?.[AppConsts.localizationSourceName] ?? {};
            AppConsts.settings = result.setting?.values ?? {};
            AppConsts.recaptchaSiteKey = AppConsts.settings['Recaptcha.SiteKey'];
        } catch (error) {
            console.warn('Careers portal: could not load ABP user configuration.', error);
        }
    }
}
