import { Routes } from '@angular/router';
import { authGuard } from '@core/auth.guard';

/**
 * Every route is lazily loaded. On a public site the first paint is the vacancy list, and a job seeker who
 * never signs in should not download the profile/CV editors at all.
 */
export const APP_ROUTES: Routes = [
    {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
            import('@pages/vacancies/vacancy-list.component').then((m) => m.VacancyListComponent),
    },
    {
        path: 'jobs/:id',
        loadComponent: () =>
            import('@pages/vacancies/vacancy-detail.component').then((m) => m.VacancyDetailComponent),
    },
    {
        path: 'jobs/:id/apply',
        canActivate: [authGuard],
        loadComponent: () => import('@pages/vacancies/apply.component').then((m) => m.ApplyComponent),
    },

    {
        path: 'account/login',
        loadComponent: () => import('@pages/account/login.component').then((m) => m.LoginComponent),
    },
    {
        path: 'account/register',
        loadComponent: () => import('@pages/account/register.component').then((m) => m.RegisterComponent),
    },
    {
        path: 'account/forgot-password',
        loadComponent: () =>
            import('@pages/account/forgot-password.component').then((m) => m.ForgotPasswordComponent),
    },
    {
        path: 'account/reset-password',
        loadComponent: () =>
            import('@pages/account/reset-password.component').then((m) => m.ResetPasswordComponent),
    },

    {
        path: 'my/profile',
        canActivate: [authGuard],
        loadComponent: () => import('@pages/candidate/profile.component').then((m) => m.ProfileComponent),
    },
    {
        path: 'my/cvs',
        canActivate: [authGuard],
        loadComponent: () => import('@pages/candidate/cv-list.component').then((m) => m.CvListComponent),
    },
    {
        path: 'my/cvs/new',
        canActivate: [authGuard],
        loadComponent: () => import('@pages/candidate/cv-edit.component').then((m) => m.CvEditComponent),
    },
    {
        path: 'my/cvs/:id',
        canActivate: [authGuard],
        loadComponent: () => import('@pages/candidate/cv-edit.component').then((m) => m.CvEditComponent),
    },
    {
        path: 'my/applications',
        canActivate: [authGuard],
        loadComponent: () =>
            import('@pages/candidate/applications.component').then((m) => m.ApplicationsComponent),
    },

    { path: '**', redirectTo: '' },
];
