import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { AuthService } from './auth.service';

/**
 * Attaches the candidate's bearer token to every API call and signs them out on a 401.
 *
 * A 401 on the careers surface means the token expired -- the candidate endpoints are `[AbpAuthorize]`d, so
 * an anonymous call to them is a 401, not a 403. Sending the candidate back to the sign-in page with a
 * `returnUrl` is better than letting the page render empty.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const token = authService.token;

    const authorized = token
        ? req.clone({ setHeaders: { Authorization: 'Bearer ' + token } })
        : req;

    return next(authorized).pipe(
        tap({
            error: (error: unknown) => {
                if (error instanceof HttpErrorResponse && error.status === 401 && token) {
                    authService.logout();
                    router.navigate(['/account/login'], {
                        queryParams: { returnUrl: router.url },
                    });
                }
            },
        })
    );
};
