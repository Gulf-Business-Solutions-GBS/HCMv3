import { CareersEnvironment } from './environment.model';

/**
 * DEVELOPMENT configuration for the careers portal.
 *
 * `remoteServiceBaseUrlFormat` deliberately contains a `{HOST}` placeholder rather than a fixed host name.
 * The careers API resolves its tenant from the **Host header of the API request** (see
 * `HCMv3.Careers.CareersTenantResolver` and the `App:CareersDomainFormat` configuration key), so the portal
 * MUST call the API on the same tenant subdomain the browser is sitting on. Hard-coding
 * `https://localhost:44301` would send `Host: localhost`, which matches no `{TENANCY_NAME}.localhost`
 * pattern, and every careers endpoint would fail closed with "portal not recognized".
 *
 * `{HOST}` is substituted with `window.location.hostname` at boot, so browsing the portal at
 * `http://acme.localhost:4300` makes API calls to `https://acme.localhost:44301`.
 */
export const environment: CareersEnvironment = {
    production: false,
    remoteServiceBaseUrlFormat: 'https://{HOST}:44301',
    appBaseUrlFormat: 'http://{HOST}:4300',
};
