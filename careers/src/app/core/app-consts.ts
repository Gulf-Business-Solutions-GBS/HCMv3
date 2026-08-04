/**
 * Runtime constants for the careers portal, populated by `CareersPreBootstrap` before Angular bootstraps.
 * Mirrors the role of `AppConsts` in the admin SPA, but carries only what a public portal needs.
 */
export class AppConsts {
    /** Resolved from `environment.remoteServiceBaseUrlFormat` with `{HOST}` substituted. */
    static remoteServiceBaseUrl: string;

    /** Resolved from `environment.appBaseUrlFormat` with `{HOST}` substituted. */
    static appBaseUrl: string;

    /** The ABP localization source that carries this application's texts. */
    static readonly localizationSourceName = 'HCMv3';

    /** Key used to persist the access token in localStorage. */
    static readonly authTokenName = 'careersAccessToken';

    static readonly encryptedAuthTokenName = 'careersEncryptedAccessToken';

    /** Google reCAPTCHA v3 site key, read from the tenant's `Recaptcha.SiteKey` setting. */
    static recaptchaSiteKey: string;

    /** ABP setting values as returned by `/AbpUserConfiguration/GetAll`. */
    static settings: { [key: string]: string } = {};

    /** ABP localization values for `localizationSourceName`. */
    static localization: { [key: string]: string } = {};

    static readonly settingNames = {
        useCaptchaOnRegistration: 'App.UserManagement.UseCaptchaOnRegistration',
        useCaptchaOnResetPassword: 'App.UserManagement.UseCaptchaOnPasswordReset',
        allowCandidateSelfRegistration: 'App.Careers.AllowCandidateSelfRegistration',
    };
}
