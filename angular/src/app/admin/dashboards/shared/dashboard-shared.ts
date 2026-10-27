import { DashboardScopeDto, DashboardScopeReason } from '@shared/service-proxies/service-proxies';

/** Period shortcuts in every dashboard's filter window. */
export type PeriodPreset = 'yearToDate' | 'thisYear' | 'lastYear' | 'thisMonth' | 'custom';

export const PERIOD_PRESETS: PeriodPreset[] = ['yearToDate', 'thisYear', 'lastYear', 'thisMonth', 'custom'];

/** The dates a preset stands for, worked out on the day it is picked. Custom keeps whatever was there. */
export function periodOf(preset: PeriodPreset): { from: Date; to: Date } {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const year = now.getFullYear();

    switch (preset) {
        case 'thisYear':
            return { from: new Date(year, 0, 1), to: new Date(year, 11, 31) };
        case 'lastYear':
            return { from: new Date(year - 1, 0, 1), to: new Date(year - 1, 11, 31) };
        case 'thisMonth':
            return { from: new Date(year, now.getMonth(), 1), to: new Date(year, now.getMonth() + 1, 0) };
        default:
            return { from: new Date(year, 0, 1), to: today };
    }
}

/** Localization key for a preset's label. */
export function presetLabelKey(preset: PeriodPreset): string {
    switch (preset) {
        case 'thisYear':
            return 'LeaveDashboard_ThisYear';
        case 'lastYear':
            return 'LeaveDashboard_LastYear';
        case 'thisMonth':
            return 'ThisMonth';
        case 'custom':
            return 'Custom';
        default:
            return 'LeaveDashboard_YearToDate';
    }
}

/**
 * "All employees (Admin)", "Finance + Payroll + your team", ... -- the server's reason for whose data a dashboard
 * shows, in words. `l` is the component's localize function.
 */
export function scopeLabel(scope: DashboardScopeDto | undefined, l: (key: string) => string): string {
    if (!scope) {
        return '';
    }

    switch (scope.reason) {
        case DashboardScopeReason.Admin:
            return l('Dashboard_ScopeAllAdmin');
        case DashboardScopeReason.Nobody:
            return l('Dashboard_ScopeNobody');
        default: {
            const parts = [...(scope.orgUnitNames ?? [])];
            if (scope.includesOwnTeam) {
                parts.push(l('Dashboard_ScopeYourTeam'));
            }
            return parts.join(' + ');
        }
    }
}
