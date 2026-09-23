import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { AppRouteGuard } from './auth-route-guard';

/**
 * Where the application opens after login.
 *
 * Legacy `WebApplication.SetupStartupDashboard` read `ManageGeneralSettings.StartupDashboard` and made that
 * view the startup navigation item. Here the setting holds an application route (for example
 * `/app/main/home`), sent with the session as `tenant.startupDashboard`; a blank or foreign value falls
 * back to the usual best route for the user's permissions. Only in-app routes are honoured, so a mistyped
 * setting cannot send people off the site.
 */
export const startupDashboardGuard: CanActivateFn = (): UrlTree => {
    const router = inject(Router);
    const session = inject(AppSessionService);
    const routeGuard = inject(AppRouteGuard);

    // Typed loosely so this compiles before and after the proxies are regenerated with the new field.
    const tenant = session.tenant as { startupDashboard?: string } | undefined;
    const configured = (tenant?.startupDashboard || '').trim();
    if (configured.startsWith('/app/')) {
        return router.parseUrl(configured);
    }

    return router.parseUrl(routeGuard.selectBestRoute());
};
