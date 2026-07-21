import { Routes } from '@angular/router';
import { appRoutes } from './app/app.routes';

export const routes: Routes = [
    { path: '', redirectTo: '/app/main/dashboard', pathMatch: 'full' },
    {
        path: 'account',
        loadComponent: () => import('account/account.component').then((m) => m.AccountComponent),
        data: { preload: true },
        children: [
            { path: '', redirectTo: 'login', pathMatch: 'full' },
            {
                path: 'login',
                loadComponent: () => import('account/login/login.component').then((m) => m.LoginComponent),
            },
            {
                path: 'register',
                loadComponent: () => import('account/register/register.component').then((m) => m.RegisterComponent),
            },
            {
                path: 'register-tenant',
                loadComponent: () =>
                    import('account/register/register-tenant.component').then((m) => m.RegisterTenantComponent),
            },
            {
                path: 'register-tenant-result',
                loadComponent: () =>
                    import('account/register/register-tenant-result.component').then(
                        (m) => m.RegisterTenantResultComponent
                    ),
            },
            {
                path: 'forgot-password',
                loadComponent: () =>
                    import('account/password/forgot-password.component').then((m) => m.ForgotPasswordComponent),
            },
            {
                path: 'reset-password',
                loadComponent: () =>
                    import('account/password/reset-password.component').then((m) => m.ResetPasswordComponent),
            },
            {
                path: 'email-activation',
                loadComponent: () =>
                    import('account/email-activation/email-activation.component').then(
                        (m) => m.EmailActivationComponent
                    ),
            },
            {
                path: 'change-email',
                loadComponent: () =>
                    import('account/change-email/change-email.component').then((m) => m.ChangeEmailComponent),
            },
            {
                path: 'confirm-email',
                loadComponent: () =>
                    import('account/email-activation/confirm-email.component').then((m) => m.ConfirmEmailComponent),
            },
            {
                path: 'send-code',
                loadComponent: () =>
                    import('account/login/send-two-factor-code.component').then((m) => m.SendTwoFactorCodeComponent),
            },
            {
                path: 'verify-code',
                loadComponent: () =>
                    import('account/login/validate-two-factor-code.component').then(
                        (m) => m.ValidateTwoFactorCodeComponent
                    ),
            },
            {
                path: 'verify-passwordless-login',
                loadComponent: () =>
                    import('account/login/validate-passwordless-login.component').then(
                        (m) => m.ValidatePasswordlessLoginComponent
                    ),
            },
            {
                path: 'passwordless-login',
                loadComponent: () =>
                    import('account/login/passwordless-login.component').then((m) => m.PasswordlessLoginComponent),
            },
            {
                path: 'gateway-selection',
                loadComponent: () =>
                    import('account/payment/gateway-selection.component').then((m) => m.GatewaySelectionComponent),
            },
            {
                path: 'select-edition',
                loadComponent: () =>
                    import('account/register/select-edition.component').then((m) => m.SelectEditionComponent),
            },
            {
                path: 'paypal-pre-payment',
                loadComponent: () =>
                    import('account/payment/paypal/paypal-pre-payment.component').then(
                        (m) => m.PayPalPrePaymentComponent
                    ),
            },
            {
                path: 'stripe-pre-payment',
                loadComponent: () =>
                    import('account/payment/stripe/stripe-pre-payment.component').then(
                        (m) => m.StripePrePaymentComponent
                    ),
            },
            {
                path: 'stripe-post-payment',
                loadComponent: () =>
                    import('account/payment/stripe/stripe-post-payment.component').then(
                        (m) => m.StripePostPaymentComponent
                    ),
            },
            {
                path: 'stripe-cancel-payment',
                loadComponent: () =>
                    import('account/payment/stripe/stripe-cancel-payment.component').then(
                        (m) => m.StripeCancelPaymentComponent
                    ),
            },
            {
                path: 'buy-succeed',
                loadComponent: () =>
                    import('account/buy-succeed/buy-succeed.component').then((m) => m.BuySucceedComponent),
            },
            {
                path: 'extend-succeed',
                loadComponent: () =>
                    import('account/extend-succeed/extend-succeed.component').then((m) => m.ExtendSucceedComponent),
            },
            {
                path: 'upgrade-succeed',
                loadComponent: () =>
                    import('account/upgrade-succeed/upgrade-succeed.component').then((m) => m.UpgradeSucceedComponent),
            },
            {
                path: 'session-locked',
                loadComponent: () =>
                    import('account/login/session-lock-screen.component').then((m) => m.SessionLockScreenComponent),
            },
            { path: '**', redirectTo: 'login' },
        ],
    },
    ...appRoutes,
    {
        path: 'could-not-connect-server',
        loadComponent: () =>
            import('connection-failed/connection-failed.component').then((m) => m.ConnectionFailedComponent),
    },
    {
        path: 'install',
        loadComponent: () => import('install/install.component').then((m) => m.InstallComponent),
    },
    { path: '**', redirectTo: '/app/main/dashboard' },
];
