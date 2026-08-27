import { ajaxSetup, fetchSetup } from '@devexpress/analytics-core/analytics-utils';

/**
 * Installs the app's ABP credentials into the DevExpress reporting client's OWN request pipeline.
 *
 * WHY THIS EXISTS. The DevExpress Report Designer / Web Document Viewer widgets do not use Angular's
 * `HttpClient` -- they issue their own jQuery and `fetch` requests directly. `ZeroTemplateHttpInterceptor`
 * therefore never sees them and never attaches the bearer token, so once the backend controllers
 * (`/DXXRD`, `/DXXRDV`, `/DXXQB`) were secured with `AbpMvcAuthorize` every widget request would 401.
 * Setting the headers here is the officially supported hook and the only place they can be attached.
 *
 * WHY BOTH SETUP OBJECTS. `@devexpress/analytics-core` routes some operations through jQuery
 * (`ajaxSetup.ajaxSettings`) and others through `fetch` (`fetchSetup.fetchSettings`), and which one is
 * used varies per operation and per widget. Configuring only one produces *intermittent* 401s that look
 * like a flaky backend. Always set both.
 *
 * WHEN TO CALL IT. Before the widget mounts -- i.e. from the hosting component's constructor or field
 * initialisers, never from an `(onBeforeRender)`-style callback, which fires only after the widget has
 * already made its first request.
 *
 * Both setup objects are module-level singletons shared by every DevExpress widget on the page, so
 * calling this more than once is harmless: it simply re-stamps the current token.
 */
export function configureDevExpressReportingAuth(): void {
    const headers = buildAbpHeaders();

    // jQuery-based operations.
    ajaxSetup.ajaxSettings = {
        ...(ajaxSetup.ajaxSettings ?? {}),
        headers: {
            ...((ajaxSetup.ajaxSettings ?? {}).headers ?? {}),
            ...headers,
        },
    };

    // fetch-based operations.
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
