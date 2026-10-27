import { CareersEnvironment } from './environment.model';

/**
 * PRODUCTION configuration for the careers portal.
 *
 * Both URLs keep the `{HOST}` placeholder on purpose: in production the portal is served per-tenant from
 * `{tenancy}.careers.example.com` and must call the API on a host that matches `App:CareersDomainFormat`.
 * Replace the suffixes below with the real deployment domains.
 */
export const environment: CareersEnvironment = {
    production: true,
    remoteServiceBaseUrlFormat: 'https://{HOST}',
    appBaseUrlFormat: 'https://{HOST}',
};
