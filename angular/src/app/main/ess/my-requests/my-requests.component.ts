import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import {
    GetMyRequestListInput,
    MyRequestListItemDto,
    MyRequestStatusCountDto,
    MyRequestTypeFilterDto,
    MyRequestsServiceProxy,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';

/**
 * Every request this employee has raised, of every type, in one place.
 *
 * <p>The gap this closes: until now an employee who wanted to know where something had got to had to guess
 * which of twenty self-service screens they had raised it on, open that one, and look. There was no screen
 * that answered "what have I asked for, and what happened to it" -- which is the only question most people
 * come to self-service to ask.</p>
 *
 * <p>Cheap to build because of how workflow is stored: every request of every type is one row in
 * <c>WorkflowRequest</c> carrying its own summary, so this is one paged query rather than twenty merged in
 * the browser. The per-type screens stay, because raising a request needs the type's own fields.</p>
 *
 * <p>Unlike its twenty siblings this screen uses the PrimeNG lazy table rather than the ESS house pattern of
 * a plain table over an unpaged fetch of 50 rows. It has to: it spans every type, so it is the one ESS list
 * whose row count grows without bound, and it is the only one with server-side filters to drive.</p>
 */
@Component({
    templateUrl: './my-requests.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        Tooltip,
        EssRequestDetailModalComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class MyRequestsComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(MyRequestsServiceProxy);
    private readonly _router = inject(Router);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    readonly filterText = signal<string>('');

    /** Null is "every status" rather than a status of its own, so the chip row needs no sentinel value. */
    readonly selectedStatus = signal<RequestStatus | null>(null);

    readonly selectedType = signal<string>('');

    /** Bound to <input type="date">, so plain ISO strings; converted to luxon only when the query is built. */
    readonly fromDate = signal<string>('');
    readonly toDate = signal<string>('');

    readonly statusCounts = signal<MyRequestStatusCountDto[]>([]);
    readonly types = signal<MyRequestTypeFilterDto[]>([]);

    /**
     * The chip row, in the order the user named them: what is still being written, what is waiting, then
     * what has been decided. Listed rather than derived from the enum so the order is a decision.
     */
    readonly statusChips: RequestStatus[] = [
        RequestStatus.Draft,
        RequestStatus.Open,
        RequestStatus.Approved,
        RequestStatus.Rejected,
        RequestStatus.Closed,
        RequestStatus.Transferred,
    ];

    ngOnInit(): void {
        // Deliberately does NOT call getMyRequests(): the table's (onLazyLoad) fires the first load, and
        // calling it here as well would run the query twice on every visit.
        this.loadFilterLookups();
    }

    getMyRequests(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getMyRequests(
                new GetMyRequestListInput({
                    status: this.selectedStatus() ?? undefined,
                    requestType: this.selectedType() || undefined,
                    fromDate: this.fromDate() ? DateTime.fromISO(this.fromDate()) : undefined,
                    toDate: this.toDate() ? DateTime.fromISO(this.toDate()) : undefined,
                    filter: this.filterText(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getMyRequests({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /**
     * Chip counts and the type list.
     *
     * <p>Failure is swallowed: these only decorate the filters, and a user with no employee record gets
     * empty lists from the server anyway. Losing them must not stop the grid itself loading.</p>
     */
    loadFilterLookups(): void {
        this._service.getMyRequestStatusCounts().subscribe({
            next: (result) => this.statusCounts.set(result),
            error: () => undefined,
        });

        this._service.getMyRequestTypes().subscribe({
            next: (result) => this.types.set(result),
            error: () => undefined,
        });
    }

    /** Picking a status resets to page 1: page 4 of the old filter is not page 4 of the new one. */
    selectStatus(status: RequestStatus | null): void {
        this.selectedStatus.set(status);
        this.applyFilters();
    }

    /**
     * Re-runs the query from page one. Goes through getMyRequests() with no event so that
     * shouldResetPaging() does the page reset, which is the same path the house pattern uses -- page 4 of
     * the old filter is not page 4 of the new one.
     */
    applyFilters(): void {
        this.getMyRequests();
    }

    clearFilters(): void {
        this.filterText.set('');
        this.selectedStatus.set(null);
        this.selectedType.set('');
        this.fromDate.set('');
        this.toDate.set('');
        this.applyFilters();
    }

    statusCount(status: RequestStatus): number {
        return this.statusCounts().find((c) => c.status === status)?.count ?? 0;
    }

    totalCount(): number {
        return this.statusCounts().reduce((sum, c) => sum + (c.count ?? 0), 0);
    }

    /** "Awaiting approval" for Open -- the user's own wording, and clearer than "Open". */
    statusLabel(status: RequestStatus | undefined): string {
        if (status === undefined || status === null) {
            return '';
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[status]);
    }

    statusClass(status: RequestStatus | undefined): string {
        switch (status) {
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

    /** The catalog's display name -- "Exit Permit", not "EmployeeExitPermit". Falls back to the raw code so
     * a type with no catalog row shows something rather than an empty cell. */
    typeName(record: MyRequestListItemDto): string {
        return this.pick(record.requestTypeNameEn, record.requestTypeNameAr) || record.requestType || '';
    }

    typeFilterName(type: MyRequestTypeFilterDto): string {
        return this.pick(type.nameEn, type.nameAr) || type.requestType || '';
    }

    summary(record: MyRequestListItemDto): string {
        return this.pick(record.summaryEn, record.summaryAr);
    }

    /**
     * Opens the shared read-only request detail, the same one the per-type screens use.
     *
     * <p>Generic by necessity: this list knows the workflow row, not the transaction behind it, so the
     * fields are the ones every request has. The approval trail -- which is what someone opening this screen
     * is usually after -- is loaded by the modal itself from the workflow request id, and that is the same
     * whatever the type.</p>
     */
    openDetail(record: MyRequestListItemDto): void {
        this.detail()?.show({
            title: this.typeName(record) || this.l('EssMyRequests'),
            fields: [
                { label: this.l('RequestNumber'), value: record.requestNumber ?? '' },
                { label: this.l('RequestType'), value: this.typeName(record) },
                { label: this.l('SubmittedOn'), value: record.creationTime?.toFormat('yyyy-MM-dd HH:mm') ?? '' },
                { label: this.l('Step'), value: record.workflowStep ? String(record.workflowStep) : '' },
            ],
            comments: this.summary(record),
            // The row id IS the WorkflowRequest id, which is what the modal loads its activity feed from.
            workflowRequestId: record.id,
            statusLabel: this.statusLabel(record.requestStatus),
            statusClass: this.statusClass(record.requestStatus),
        });
    }

    /** Whether this row can be opened on the type's own screen, where the transaction's own fields live. */
    hasOwnScreen(record: MyRequestListItemDto): boolean {
        return !!record.essRoute;
    }

    openOwnScreen(record: MyRequestListItemDto): void {
        if (record.essRoute) {
            this._router.navigate([record.essRoute]);
        }
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }

    /** Bilingual names follow the UI language; the two fields are maintained separately. */
    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }
}
