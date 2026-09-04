import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EssContextDto,
    EssRecentRequestDto,
    EssSessionServiceProxy,
    EssTransactionTypeDto,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../shared/common/workflow-activity/workflow-history-modal.component';

/**
 * The Employee Self Service landing page.
 *
 * <p>The tile list is NOT hardcoded: it comes from the WorkflowTransactionType catalog via GetEssContext, so
 * enabling a seventh self-service transaction is a catalog row plus a form, not a change here. That is also
 * why this lives in the admin application at all -- the admin menu is permission-filtered, so an employee
 * holding only the Employee role never sees it, and legacy HCM10's separate ESS project is not needed.</p>
 *
 * <p>The two counts are deliberately shown as separate cards rather than one "pending" number. Most people
 * are on both sides of a workflow, and what you are waiting on somebody else for is a completely different
 * thing from what somebody else is waiting on you for.</p>
 */
@Component({
    selector: 'ess-home',
    templateUrl: './ess-home.component.html',
    styleUrls: ['./ess-home.component.less'],
    animations: [appModuleAnimation()],
    imports: [CommonModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, RouterModule, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssHomeComponent extends AppComponentBase implements OnInit {
    /**
     * True when rendered inside the dashboard rather than as its own page.
     *
     * Suppresses the page chrome -- sub-header and container -- because the dashboard supplies both.
     * Kept as a flag rather than splitting the component in two: the ESS logic here (recent requests,
     * what is waiting on you, workflow history) is the same either way, and two copies of it would drift.
     */
    readonly embedded = input(false);

    private readonly _essSessionService = inject(EssSessionServiceProxy);

    readonly loading = signal(true);
    readonly context = signal<EssContextDto | undefined>(undefined);

    readonly historyModal = viewChild<WorkflowHistoryModalComponent>('history');

    readonly tiles = computed(() => this.context()?.availableTransactionTypes ?? []);
    readonly recent = computed(() => this.context()?.recentRequests ?? []);

    ngOnInit(): void {
        this.loadContext();
    }

    loadContext(): void {
        this.loading.set(true);

        this._essSessionService
            .getEssContext()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.context.set(result));
    }

    /**
     * Employee names follow the UI language rather than being transliterated -- the English and Arabic name
     * fields are maintained separately on Employee.
     */
    employeeName(): string {
        const ctx = this.context();
        if (!ctx) {
            return '';
        }

        return this.isRtl ? ctx.employeeFullNameAr || ctx.employeeFullNameEn : ctx.employeeFullNameEn;
    }

    /** Initials stand in for a missing photo, so the card never shows a broken image or an empty circle. */
    initials(): string {
        // ?? '' because employeeName() legitimately returns null -- an unlinked account has no name, and
        // a template that reaches this while the context says so would otherwise throw on every
        // change-detection pass, taking the rest of the page's rendering down with it.
        const name = (this.employeeName() ?? '').trim();
        if (!name) {
            return '?';
        }

        const parts = name.split(/\s+/);
        const first = parts[0]?.charAt(0) ?? '';
        const last = parts.length > 1 ? parts[parts.length - 1].charAt(0) : '';

        return (first + last).toUpperCase();
    }

    photoUrl(): string | undefined {
        const ctx = this.context();

        return ctx?.hasPhoto && ctx.employeeId ? `/api/employee-photo/${ctx.employeeId}` : undefined;
    }

    typeName(type: EssTransactionTypeDto): string {
        return this.isRtl ? type.nameAr || type.nameEn : type.nameEn;
    }

    requestTypeName(request: EssRecentRequestDto): string {
        return this.isRtl
            ? request.requestTypeNameAr || request.requestTypeNameEn || request.requestType
            : request.requestTypeNameEn || request.requestType;
    }

    requestSummary(request: EssRecentRequestDto): string {
        return this.isRtl ? request.summaryAr || request.summaryEn : request.summaryEn;
    }

    statusLabel(request: EssRecentRequestDto): string {
        return this.l('WorkflowRequestStatus_' + RequestStatus[request.requestStatus]);
    }

    statusClass(request: EssRecentRequestDto): string {
        switch (request.requestStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
                return 'badge-danger';
            default:
                return 'badge-secondary';
        }
    }

    openHistory(request: EssRecentRequestDto): void {
        this.historyModal()?.show(request.id);
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
