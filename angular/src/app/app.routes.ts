import { Routes } from '@angular/router';
import { AppRouteGuard } from './shared/common/auth/auth-route-guard';

export const appRoutes: Routes = [
    {
        path: 'app',
        loadComponent: () => import('./app.component').then((m) => m.AppComponent),
        canActivate: [AppRouteGuard],
        canActivateChild: [AppRouteGuard],
        children: [
            {
                path: '',
                children: [
                    {
                        path: 'notifications',
                        loadComponent: () =>
                            import('./shared/layout/notifications/notifications.component').then(
                                (m) => m.NotificationsComponent
                            ),
                    },
                    { path: '', redirectTo: '/app/main/dashboard', pathMatch: 'full' },
                ],
            },
            {
                path: 'main',
                data: { preload: true },
                children: [
                    {
                        path: 'dashboard',
                        loadComponent: () =>
                            import('./main/dashboard/dashboard.component').then((m) => m.DashboardComponent),
                        data: { permission: 'Pages.Tenant.Dashboard' },
                    },
                    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
                ],
            },
            {
                path: 'admin',
                data: { preload: true },
                canLoad: [AppRouteGuard],
                children: [
                    {
                        path: 'users',
                        loadComponent: () => import('./admin/users/users.component').then((m) => m.UsersComponent),
                        data: { permission: 'Pages.Administration.Users' },
                    },
                    {
                        path: 'roles',
                        loadComponent: () => import('./admin/roles/roles.component').then((m) => m.RolesComponent),
                        data: { permission: 'Pages.Administration.Roles' },
                    },
                    {
                        path: 'auditLogs',
                        loadComponent: () =>
                            import('./admin/audit-logs/audit-logs.component').then((m) => m.AuditLogsComponent),
                        data: { permission: 'Pages.Administration.AuditLogs' },
                    },
                    {
                        path: 'entity-changes/:entityId/:entityTypeFullName',
                        loadComponent: () =>
                            import('./admin/entity-changes/entity-changes.component').then(
                                (m) => m.EntityChangesComponent
                            ),
                        data: { permission: 'Pages.Administration.EntityChanges.FullHistory' },
                    },
                    {
                        path: 'maintenance',
                        loadComponent: () =>
                            import('./admin/maintenance/maintenance.component').then((m) => m.MaintenanceComponent),
                        data: { permission: 'Pages.Administration.Host.Maintenance' },
                    },
                    {
                        path: 'health-check-ui',
                        loadComponent: () =>
                            import('./admin/health-check-ui/health-check-ui.component').then(
                                (m) => m.HealthCheckUIComponent
                            ),
                        data: { permission: 'Pages.Administration.Host.HealthCheck' },
                    },
                    {
                        path: 'hostSettings',
                        loadComponent: () =>
                            import('./admin/settings/host-settings.component').then((m) => m.HostSettingsComponent),
                        data: { permission: 'Pages.Administration.Host.Settings' },
                    },
                    {
                        path: 'active-sessions',
                        loadComponent: () =>
                            import('./admin/active-sessions/active-sessions.component').then(
                                (m) => m.ActiveSessionsComponent
                            ),
                        data: { permission: 'Pages.Administration.ActiveSessions' },
                    },
                    {
                        path: 'editions',
                        loadComponent: () =>
                            import('./admin/editions/editions.component').then((m) => m.EditionsComponent),
                        data: { permission: 'Pages.Editions' },
                    },
                    {
                        path: 'languages',
                        loadComponent: () =>
                            import('./admin/languages/languages.component').then((m) => m.LanguagesComponent),
                        data: { permission: 'Pages.Administration.Languages' },
                    },
                    {
                        path: 'languages/:name/texts',
                        loadComponent: () =>
                            import('./admin/languages/language-texts.component').then((m) => m.LanguageTextsComponent),
                        data: { permission: 'Pages.Administration.Languages.ChangeTexts' },
                    },
                    {
                        path: 'tenants',
                        loadComponent: () =>
                            import('./admin/tenants/tenants.component').then((m) => m.TenantsComponent),
                        data: { permission: 'Pages.Tenants' },
                    },
                    {
                        path: 'organization-units',
                        loadComponent: () =>
                            import('./admin/organization-units/organization-units.component').then(
                                (m) => m.OrganizationUnitsComponent
                            ),
                        data: { permission: 'Pages.Administration.OrganizationUnits' },
                    },
                    {
                        path: 'subscription-management',
                        loadComponent: () =>
                            import('./admin/subscription-management/subscription-management.component').then(
                                (m) => m.SubscriptionManagementComponent
                            ),
                        data: { permission: 'Pages.Administration.Tenant.SubscriptionManagement' },
                    },
                    {
                        path: 'invoice/:paymentId',
                        loadComponent: () =>
                            import('./admin/subscription-management/invoice/invoice.component').then(
                                (m) => m.InvoiceComponent
                            ),
                        data: { permission: 'Pages.Administration.Tenant.SubscriptionManagement' },
                    },
                    {
                        path: 'tenantSettings',
                        loadComponent: () =>
                            import('./admin/settings/tenant-settings.component').then((m) => m.TenantSettingsComponent),
                        data: { permission: 'Pages.Administration.Tenant.Settings' },
                    },
                    {
                        path: 'hostDashboard',
                        loadComponent: () =>
                            import('./admin/dashboard/host-dashboard.component').then((m) => m.HostDashboardComponent),
                        data: { permission: 'Pages.Administration.Host.Dashboard' },
                    },
                    {
                        path: 'demo-ui-components',
                        loadComponent: () =>
                            import('./admin/demo-ui-components/demo-ui-components.component').then(
                                (m) => m.DemoUiComponentsComponent
                            ),
                        data: { permission: 'Pages.DemoUiComponents' },
                    },
                    {
                        path: 'ui-customization',
                        loadComponent: () =>
                            import('./admin/ui-customization/ui-customization.component').then(
                                (m) => m.UiCustomizationComponent
                            ),
                        data: {},
                    },
                    {
                        path: 'webhook-subscriptions',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-subscription.component').then(
                                (m) => m.WebhookSubscriptionComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription' },
                    },
                    {
                        path: 'webhook-subscriptions-detail',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-subscription-detail.component').then(
                                (m) => m.WebhookSubscriptionDetailComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription.Detail' },
                    },
                    {
                        path: 'webhook-event-detail',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-event-detail.component').then(
                                (m) => m.WebhookEventDetailComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription.Detail' },
                    },
                    {
                        path: 'dynamic-property',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-property.component').then(
                                (m) => m.DynamicPropertyComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'dynamic-entity-property/:entityFullName',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-entity-properties/dynamic-entity-property-list.component').then(
                                (m) => m.DynamicEntityPropertyListComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'dynamic-entity-property-value/manage-all/:entityFullName/:rowId',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-entity-properties/value/dynamic-entity-property-value.component').then(
                                (m) => m.DynamicEntityPropertyValueComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'login-attempts',
                        loadComponent: () =>
                            import('./admin/login-attempts/login-attempts.component').then(
                                (m) => m.LoginAttemptsComponent
                            ),
                        data: {},
                    },
                    {
                        path: 'mass-notifications',
                        loadComponent: () =>
                            import('./admin/mass-notifications/mass-notifications.component').then(
                                (m) => m.MassNotificationsComponent
                            ),
                        data: { permission: 'Pages.Administration.MassNotification' },
                    },
                    { path: '', redirectTo: 'hostDashboard', pathMatch: 'full' },
                    { path: '**', redirectTo: 'hostDashboard' },
                ],
            },
            {
                path: '**',
                redirectTo: 'notifications',
            },
        ],
    },
];
