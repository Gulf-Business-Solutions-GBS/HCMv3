import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Gate for the authenticated candidate area (profile, CVs, applications).
 *
 * This is a convenience only -- it prevents rendering a page that would immediately 401. The real
 * enforcement is server-side: every method on `ICareersCandidateAppService` resolves the applicant from
 * `Applicant.UserId == AbpSession.UserId` and never accepts an applicant id from the client.
 */
export const authGuard: CanActivateFn = (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.isAuthenticated()) {
        return true;
    }

    return router.createUrlTree(['/account/login'], {
        queryParams: { returnUrl: state.url },
    });
};
