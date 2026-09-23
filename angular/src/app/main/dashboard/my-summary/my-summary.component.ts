import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EssInboxServiceProxy,
    MyDashboardLeaveBalanceDto,
    MyDashboardServiceProxy,
    MyDashboardSummaryDto,
} from '@shared/service-proxies/service-proxies';

interface QuickAction {
    labelKey: string;
    icon: string;
    route: string;
    permission?: string;

    /**
     * Marks the one tile that carries a live count.
     *
     * A number belongs on a tile only when acting on it is the whole point of the tile. "Six documents"
     * is trivia; "one approval waiting on you" is a task, and it is the single thing on this page that
     * somebody else is blocked on.
     */
    showsPendingApprovals?: boolean;
}

/**
 * The dashboard's personal tiles: who you are, where you work, what you can do, and what leave you have
 * left.
 *
 * MODELLED ON THE LEGACY ESS DASHBOARD, on the main dashboard rather than a separate ESS home -- one
 * landing page, so nobody has to learn which of two dashboards holds the thing they want.
 *
 * The visual structure is the legacy one deliberately, after a side-by-side comparison went against the
 * earlier build: identity and employment details as two cards across the top, then Quick Actions as
 * full square tiles rather than text links, then leave / attendance / alerts as a band of three. The
 * complaint that decided it was that the icons "are clear in legacy" -- they were 22px glyphs beside
 * 12px labels here, which reads as decoration rather than as a target to hit.
 *
 * Everything arrives in ONE call. Four tiles fetching separately would be four round trips before the
 * page settles, and each would have to resolve "who am I" again. The approvals count is the single
 * exception and is explained where it is fetched.
 */
@Component({
    selector: 'my-summary',
    templateUrl: './my-summary.component.html',
    styleUrl: './my-summary.component.css',
    imports: [RouterLink, LocalizePipe, DatePipe],
})
export class MySummaryComponent extends AppComponentBase implements OnInit {
    private _dashboardService = inject(MyDashboardServiceProxy);
    private _essInboxService = inject(EssInboxServiceProxy);

    readonly summary = signal<MyDashboardSummaryDto | undefined>(undefined);
    readonly isLoading = signal<boolean>(true);

    /**
     * How many requests are waiting for THIS person to decide.
     *
     * Fetched separately from the summary on purpose: the count comes from the workflow inbox, which is
     * a different bounded piece of the system from the HR record the summary reads, and folding it into
     * GetSummary would make every dashboard load drag the workflow tables in behind it. It is also the
     * one figure that goes stale while somebody sits on the page.
     *
     * Zero is NOT rendered. An always-present "0" badge trains people to ignore the badge, and then the
     * day it says 1 they do not see it either.
     */
    readonly pendingApprovalCount = signal<number>(0);

    readonly profile = computed(() => this.summary()?.profile);
    readonly leaveBalances = computed(() => this.summary()?.leaveBalances ?? []);
    readonly alerts = computed(() => this.summary()?.alerts);
    readonly attendance = computed(() => this.summary()?.todayAttendance);
    readonly announcements = computed(() => this.summary()?.announcements ?? []);

    /**
     * Fixed, common tasks -- distinct from the Shortcuts tile below, which is what YOU chose. These are
     * the things everybody does, so they are the same for everybody and do not move around.
     *
     * Permission-checked at render: an action somebody cannot perform is a dead end, and showing it
     * teaches them to expect a denial.
     *
     * ICONS ARE FLATICON CLASSES FROM THE BUNDLED METRONIC SET, not new artwork. The fonts are already
     * in vendors.bundle.css, so nothing is added to the payload; the fix for "the legacy icons are
     * clearer" was to pick glyphs that read at a glance and then draw them at tile size, which is what
     * the stylesheet now does.
     */
    readonly quickActions: QuickAction[] = [
        {
            labelKey: 'EssMyLeave',
            icon: 'flaticon2-calendar-4',
            route: '/app/main/ess/leave',
            permission: 'Ess.MyLeave',
        },
        {
            labelKey: 'EssMyAttendance',
            icon: 'flaticon-stopwatch',
            route: '/app/main/ess/attendance',
            permission: 'Ess.MyAttendance',
        },
        {
            labelKey: 'EssMyDocuments',
            icon: 'flaticon2-document',
            route: '/app/main/ess/document',
            permission: 'Ess.MyDocuments',
        },
        {
            labelKey: 'EssMyLoans',
            icon: 'flaticon-piggy-bank',
            route: '/app/main/ess/loan',
            permission: 'Ess.MyLoans',
        },
        {
            labelKey: 'EssMyOvertime',
            icon: 'flaticon2-hourglass',
            route: '/app/main/ess/overtime',
            permission: 'Ess.MyOvertime',
        },
        {
            labelKey: 'EssMyTickets',
            icon: 'flaticon2-delivery-package',
            route: '/app/main/ess/ticket',
            permission: 'Ess.MyTickets',
        },
        {
            labelKey: 'EssInbox',
            icon: 'flaticon2-check-mark',
            route: '/app/main/ess/inbox',
            permission: 'Ess.Inbox',
            showsPendingApprovals: true,
        },
    ];

    /**
     * Whether the signed-in user is an employee at all.
     *
     * An administrator account is usually NOT linked to an Employee row, and the personal tiles --
     * identity, leave, attendance -- have nothing to draw for them. Rendering nothing left a mostly
     * blank dashboard that read as broken; this drives a notice that says why instead.
     */
    readonly hasEmployeeRecord = computed(() => !!this.profile());

    /** True once loaded and there is genuinely nothing personal to show. */
    readonly showNoEmployeeNotice = computed(() => !this.isLoading() && !this.hasEmployeeRecord());

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

        this.loadPendingApprovalCount();
    }

    /**
     * Silent on failure, and deliberately so. A user without the inbox permission gets a 403 here, which
     * is the correct answer for them -- the tile is not rendered either way, and a red toast on the
     * landing page would report their own permissions to them as if something had broken.
     */
    private loadPendingApprovalCount(): void {
        if (!this.isGranted('Ess.Inbox')) {
            return;
        }

        this._essInboxService.getMyPendingCount().subscribe({
            next: (count) => this.pendingApprovalCount.set(count ?? 0),
            error: () => this.pendingApprovalCount.set(0),
        });
    }

    /**
     * The proxies type timestamps as luxon DateTime, which Angular's date pipe refuses -- it wants a
     * Date, a number or a string. Converting here keeps the template free of casts that would hide the
     * mismatch rather than fix it. Same conversion the run-history screen needed.
     */
    toDate(value: any): Date | null {
        if (!value) {
            return null;
        }

        return typeof value.toJSDate === 'function' ? value.toJSDate() : new Date(value);
    }

    /** Blank rather than "null": an unpopulated org chart should read as empty, not as an error. */
    orgValue(value: string | undefined): string {
        return value && value.trim().length ? value : '—';
    }

    /**
     * The company an employee belongs to IS the tenant on this product -- there is no Company entity
     * under it, and legacy's Employment Details panel showed the employing company in this slot. Taken
     * from the session rather than invented on the server, and falling back to the profile's own value
     * so the day a real company field exists this reads it without further change.
     */
    companyName(): string {
        const fromProfile = this.profile()?.company;
        if (fromProfile && fromProfile.trim().length) {
            return fromProfile;
        }

        return this.appSession.tenant?.name || '—';
    }

    /**
     * Leave balances to two decimals.
     *
     * DISPLAY ONLY -- the stored value is untouched and every calculation still uses it in full. The
     * balance arrives as an accrual figure like 141.505007, and six decimal places on a day count reads
     * as a machine dumping its internals rather than as an answer to "how much leave do I have".
     */
    formatBalance(value: number | undefined): string {
        return (value ?? 0).toFixed(2);
    }

    /**
     * A repeating tint per leave type, so the cards are told apart at a glance the way the legacy screen
     * did it. Index-based rather than keyed on the type name: absence types are tenant data, and a
     * lookup table here would leave every type a tenant adds uncoloured.
     */
    balanceTint(index: number): string {
        const tints = ['ms-balance-blue', 'ms-balance-green', 'ms-balance-purple', 'ms-balance-amber'];

        return tints[index % tints.length];
    }

    /** Named for the template's sake -- `track` needs a stable key and the DTO carries no id. */
    balanceKey(balance: MyDashboardLeaveBalanceDto): string {
        return balance.leaveType ?? '';
    }
}
