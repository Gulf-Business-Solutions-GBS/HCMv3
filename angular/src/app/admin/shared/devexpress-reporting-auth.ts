import { fetchSetup } from '@devexpress/analytics-core/analytics-utils';

/**
 * Installs the app's ABP credentials into the DevExpress reporting client's OWN request pipeline.
 *
 * WHY THIS EXISTS. The DevExpress Report Designer / Web Document Viewer widgets do not use Angular's
 * `HttpClient` -- they issue their own requests directly. `ZeroTemplateHttpInterceptor` therefore never
 * sees them and never attaches the bearer token, so once the backend controllers (`/DXXRD`, `/DXXRDV`,
 * `/DXXQB`) were secured with `AbpMvcAuthorize` every widget request would 401. Setting the headers
 * here is the officially supported hook and the only place they can be attached.
 *
 * *** CONFIGURE EXACTLY ONE REQUEST MANAGER -- NEVER BOTH. ***
 * `@devexpress/analytics-core` exposes `ajaxSetup` (jQuery) and `fetchSetup` (fetch). In v23.2 these are
 * mutually exclusive: touching both throws
 *
 *     Error: You cannot configure more than one type of request manager.
 *            Use either ajaxSetup or fetchSetup for configuration, but not both.
 *
 * and because that throws while the hosting component is still initialising, the widget never mounts and
 * the page renders BLANK with no network activity at all -- it looks like a broken report or a dead
 * backend, not an auth-wiring bug. An earlier revision of this file set both "so no operation is missed";
 * that was wrong and broke every print report including ones that had previously worked.
 *
 * `fetchSetup` is the correct single choice: configuring it makes fetch the request manager for ALL
 * reporting operations, so there is no jQuery path left to miss.
 *
 * WHEN TO CALL IT. Before the widget mounts -- i.e. from the hosting component's constructor or field
 * initialisers, never from an `(onBeforeRender)`-style callback, which fires only after the widget has
 * already made its first request.
 *
 * `fetchSetup` is a module-level singleton shared by every DevExpress widget on the page, so calling this
 * more than once is harmless: it simply re-stamps the current token.
 */
export function configureDevExpressReportingAuth(): void {
    const headers = buildAbpHeaders();

    fetchSetup.fetchSettings = {
        ...(fetchSetup.fetchSettings ?? {}),
        headers: {
            ...((fetchSetup.fetchSettings ?? {}).headers ?? {}),
            ...headers,
        },
    };
}

function buildAbpHeaders(): { [key: string]: string } {
    const headers: { [key: string]: string } = {};

    const token = abp.auth.getToken();
    if (token) {
        headers['Authorization'] = 'Bearer ' + token;
    }

    // `abp.multiTenancy.tenantIdCookieName` is 'Abp-TenantId' (see src/assets/abp-web-resources/abp.js);
    // the same pair is what AppAuthService and AppPreBootstrap send. Belt-and-braces: an authenticated
    // JWT already carries the tenant claim, but sending the header keeps host-user tenant switching
    // behaving the same way here as it does on every other API call.
    const tenantId = abp.multiTenancy.getTenantIdCookie();
    if (tenantId) {
        headers[abp.multiTenancy.tenantIdCookieName] = tenantId.toString();
    }

    return headers;
}
