import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { MyDashboardServiceProxy, MyDashboardSummaryDto } from '@shared/service-proxies/service-proxies';

interface QuickAction {
    labelKey: string;
    icon: string;
    route: string;
    permission?: string;
}

/**
 * The dashboard's personal tiles: who you are, what you can do, and what leave you have left.
 *
 * MODELLED ON THE LEGACY ESS DASHBOARD, on the main dashboard rather than a separate ESS home -- one
 * landing page, so nobody has to learn which of two dashboards holds the thing they want.
 *
 * Everything arrives in ONE call. Four tiles fetching separately would be four round trips before the
 * page settles, and each would have to resolve "who am I" again.
 */
@Component({
    selector: 'my-summary',
    templateUrl: './my-summary.component.html',
    styleUrl: './my-summary.component.css',
    imports: [RouterLink, LocalizePipe, DatePipe],
})
export class MySummaryComponent extends AppComponentBase implements OnInit {
    private _dashboardService = inject(MyDashboardServiceProxy);

    readonly summary = signal<MyDashboardSummaryDto | undefined>(undefined);
    readonly isLoading = signal<boolean>(true);

    readonly profile = computed(() => this.summary()?.profile);
    readonly leaveBalances = computed(() => this.summary()?.leaveBalances ?? []);
    readonly alerts = computed(() => this.summary()?.alerts);
    readonly attendance = computed(() => this.summary()?.todayAttendance);

    /**
     * Fixed, common tasks -- distinct from the Shortcuts tile, which is what YOU chose. These are the
     * things everybody does, so they are the same for everybody and do not move around.
     *
     * Permission-checked at render: an action somebody cannot perform is a dead end, and showing it
     * teaches them to expect a denial.
     */
    readonly quickActions: QuickAction[] = [
        { labelKey: 'EssMyLeave', icon: 'flaticon-calendar', route: '/app/main/ess/leave', permission: 'Ess.MyLeave' },
        { labelKey: 'EssMyAttendance', icon: 'flaticon-time-3', route: '/app/main/ess/attendance', permission: 'Ess.MyAttendance' },
        { labelKey: 'EssMyDocuments', icon: 'flaticon-file-2', route: '/app/main/ess/document', permission: 'Ess.MyDocuments' },
        { labelKey: 'EssMyLoans', icon: 'flaticon-piggy-bank', route: '/app/main/ess/loan', permission: 'Ess.MyLoans' },
        { labelKey: 'EssMyOvertime', icon: 'flaticon-alarm', route: '/app/main/ess/overtime', permission: 'Ess.MyOvertime' },
        { labelKey: 'EssMyTickets', icon: 'flaticon-price-tag', route: '/app/main/ess/ticket', permission: 'Ess.MyTickets' },
        { labelKey: 'EssInbox', icon: 'flaticon-multimedia', route: '/app/main/ess/inbox', permission: 'Ess.Inbox' },
    ];

    readonly visibleQuickActions = computed(() =>
        this.quickActions.filter((a) => !a.permission || this.isGranted(a.permission))
    );

    ngOnInit(): void {
        this._dashboardService.getSummary().subscribe({
            next: (result) => {
                this.summary.set(result);
                this.isLoading.set(false);
            },
            error: () => {
                // The rest of the dashboard must still render. A failed summary is a missing tile, not a
                // broken page.
                this.isLoading.set(false);
            },
        });
    }

    /** Blank rather than "null": an unpopulated org chart should read as empty, not as an error. */
    orgValue(value: string | undefined): string {
        return value && value.trim().length ? value : '—';
    }
}
