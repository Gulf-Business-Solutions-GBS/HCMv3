export interface CareersEnvironment {
    production: boolean;

    /**
     * URL of the HCMv3 Web.Host API. May contain the `{HOST}` placeholder, which is replaced at boot with
     * `window.location.hostname` so that the API is always called on the same tenant subdomain as the
     * portal itself. See `environment.ts` for why that matters.
     */
    remoteServiceBaseUrlFormat: string;

    /** Base URL of the portal itself. May contain `{HOST}`. Used to build password-reset links. */
    appBaseUrlFormat: string;
}
