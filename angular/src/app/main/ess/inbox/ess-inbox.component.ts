import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    ApproveRequestInput,
    EssInboxItemDto,
    EssInboxServiceProxy,
    GetMyInboxInput,
    RejectRequestInput,
    SendBackRequestInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';

/** One inbox row, flattened for the grid: sortable scalars rather than nested bilingual pairs. */
export interface EssInboxRow {
    item: EssInboxItemDto;
    id: number;
    requestType: string;
    typeName: string;
    requestNumber: string;
    employeeId: number | undefined;
    employeeName: string;
    photoUrl: string | undefined;
    summary: string;
    submittedOn: DateTime | undefined;
    submittedOnMs: number;
    stepNumber: number;
    stepLabel: string;
    statusKey: string;
    statusLabel: string;
    statusClass: string;
    canReject: boolean;
    canSendBack: boolean;
}

/**
 * The approver's inbox: everything waiting on this person, of every transaction type, in one grid.
 *
 * <p>Rebuilt from a two-pane list because that pane showed the approver a title and a number and asked them
 * to decide. Legacy answers the same question with a grid -- request number, employee, type, status,
 * pending since -- that filters and sorts, and whose action opens the request's OWN form, read-only. This is
 * that shape: <see cref="EssRequestDetailModalComponent"/> is the very component the requester opens on
 * their own request, so approver and requester read the identical page.</p>
 *
 * <p><b>Why the filtering is done here rather than by the server.</b> <c>GetMyInboxInput</c> accepts a
 * request type and nothing else -- no employee, no date range, no status. Filtering by employee or by date
 * server-side would need fields on that DTO, and a DTO change is only real once the proxies are
 * regenerated against a rebuilt backend. So the page fetches the pending set once (it is, by definition,
 * only what is waiting on one person) and filters, sorts and pages it here. Should the inbox ever outgrow
 * <see cref="MaxInboxRows"/> the fetch is the thing to move server-side, not the grid.</p>
 */
@Component({
    templateUrl: './ess-inbox.component.html',
    styleUrls: ['./ess-inbox.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        BusyIfDirective,
        SubHeaderComponent,
        EmployeeAvatarComponent,
        EssRequestDetailModalComponent,
        HelpPageDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssInboxComponent extends AppComponentBase {
    /**
     * One person's pending approvals, fetched in a single page. Deliberately generous: an approver with
     * more than this waiting has a workload problem the grid cannot fix, and the count is capped rather
     * than silently truncated -- {@link truncated} says so on screen.
     */
    static readonly MaxInboxRows = 500;

    private readonly _inboxService = inject(EssInboxServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    readonly acting = signal(false);
    readonly notLinked = signal(false);
    readonly truncated = signal(false);

    /** Every pending row as fetched, before any filter. The grid renders a filtered slice of this. */
    readonly allRows = signal<EssInboxRow[]>([]);

    readonly selected = signal<EssInboxRow | undefined>(undefined);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    readonly filterText = signal('');
    readonly requestTypeFilter = signal('');
    readonly employeeFilter = signal('');
    readonly statusFilter = signal('');
    readonly dateFrom = signal('');
    readonly dateTo = signal('');

    /** The approver's own comment, carried on the action bar rather than in the read-only detail view. */
    comment = '';

    /** Filter dropdowns are built from what is actually waiting, so they never offer an empty result. */
    readonly requestTypeOptions = computed(() =>
        this.distinct(this.allRows().map((r) => ({ value: r.requestType, label: r.typeName })))
    );

    readonly employeeOptions = computed(() =>
        this.distinct(
            this.allRows()
                .filter((r) => r.employeeId !== undefined && r.employeeId !== null)
                .map((r) => ({ value: String(r.employeeId), label: r.employeeName }))
        )
    );

    readonly statusOptions = computed(() =>
        this.distinct(this.allRows().map((r) => ({ value: r.statusKey, label: r.statusLabel })))
    );

    readonly hasFilters = computed(
        () =>
            !!this.filterText() ||
            !!this.requestTypeFilter() ||
            !!this.employeeFilter() ||
            !!this.statusFilter() ||
            !!this.dateFrom() ||
            !!this.dateTo()
    );

    /** Whether the fetched set is still in hand, so a page or sort event need not go back to the server. */
    private loaded = false;
    private lastEvent: TableLazyLoadEvent | undefined;

    /**
     * Bound to the table's <c>(onLazyLoad)</c>, which is also what loads the page initially -- hence
     * nothing in <c>ngOnInit</c>. Fetches on the first call and after an approval; every later call
     * (sorting, paging, a filter change) re-renders what is already in hand.
     */
    getRequests(event?: TableLazyLoadEvent): void {
        this.lastEvent = event;

        if (this.loaded) {
            this.render(event);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._inboxService
            .getMyInbox(
                new GetMyInboxInput({
                    requestType: undefined,
                    maxResultCount: EssInboxComponent.MaxInboxRows,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe({
                next: (result) => {
                    this.loaded = true;
                    this.truncated.set((result.totalCount ?? 0) > (result.items?.length ?? 0));
                    this.allRows.set((result.items ?? []).map((item) => this.toRow(item)));
                    this.render(event);
                },
                // An unlinked account is a setup gap for HR to close, not a fault of theirs, so the page
                // explains it rather than showing an error toast. Admin accounts always land here: they are
                // deliberately never linked to an employee.
                error: () => {
                    this.loaded = true;
                    this.notLinked.set(true);
                },
            });
    }

    /** Re-asks the server. Used by Refresh and after any decision, since an approval may or may not finish
     * the request and only the server knows which. */
    reloadFromServer(): void {
        this.loaded = false;
        this.selected.set(undefined);
        this.allRows.set([]);
        this.getRequests(this.lastEvent);
    }

    /** Any filter change: back to the first page, then re-render. */
    applyFilters(): void {
        this.paginator()?.changePage(0);
        this.render(undefined);
    }

    clearFilters(): void {
        this.filterText.set('');
        this.requestTypeFilter.set('');
        this.employeeFilter.set('');
        this.statusFilter.set('');
        this.dateFrom.set('');
        this.dateTo.set('');
        this.applyFilters();
    }

    // ---------------------------------------------------------------- the row and what it opens

    /**
     * Opens the request the way its requester sees it: every field the inbox knows, read-only, over the
     * same Workflow Actions & Information panel. The decision itself stays on the action bar outside the
     * dialog -- that component is shared with 21 self-service screens and has no approver mode to switch
     * into.
     */
    openDetail(row: EssInboxRow): void {
        this.select(row);

        this.detail()?.show({
            title: row.typeName || this.l('EssInbox'),
            fields: [
                { label: this.l('Employee'), value: row.employeeName },
                { label: this.l('RequestType'), value: row.typeName },
                { label: this.l('RequestNumber'), value: row.requestNumber },
                { label: this.l('SubmittedOn'), value: row.submittedOn?.toFormat('dd MMM yyyy') ?? '' },
                { label: this.l('WorkflowStep'), value: row.stepLabel },
                { label: this.l('Summary'), value: row.summary },
            ],
            workflowRequestId: row.id,
            statusLabel: row.statusLabel,
            statusClass: row.statusClass,
        });
    }

    select(row: EssInboxRow): void {
        if (this.selected()?.id !== row.id) {
            this.comment = '';
        }

        this.selected.set(row);
    }

    clearSelection(): void {
        this.selected.set(undefined);
        this.comment = '';
    }

    showMenu(event: Event, row: EssInboxRow, menu: TieredMenu): void {
        this.activeMenuItems.set(this.getMenuItems(row));
        menu.toggle(event);
    }

    private getMenuItems(row: EssInboxRow): MenuItem[] {
        const items: MenuItem[] = [
            {
                label: this.l('Details'),
                icon: 'pi pi-eye',
                command: () => this.openDetail(row),
            },
            {
                label: this.l('Approve'),
                icon: 'pi pi-check',
                command: () => {
                    this.select(row);
                    this.approve();
                },
            },
        ];

        // Offered only when the step's configuration actually permits it, so the screen never shows a
        // button the engine would refuse.
        if (row.canReject) {
            items.push({
                label: this.l('Reject'),
                icon: 'pi pi-times',
                command: () => {
                    this.select(row);
                    this.reject();
                },
            });
        }

        if (row.canSendBack) {
            items.push({
                label: this.l('SendBack'),
                icon: 'pi pi-replay',
                command: () => {
                    this.select(row);
                    this.sendBack();
                },
            });
        }

        return items;
    }

    // ---------------------------------------------------------------- decisions

    approve(): void {
        const row = this.selected();
        if (!row || this.acting()) {
            return;
        }

        const input = new ApproveRequestInput();
        input.workflowRequestId = row.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.approve(input), 'RequestApproved');
    }

    reject(): void {
        const row = this.selected();
        if (!row || this.acting()) {
            return;
        }

        // Required by the server too; checked here so the approver is told before the round trip.
        if (!this.comment?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredToReject'));
            return;
        }

        const input = new RejectRequestInput();
        input.workflowRequestId = row.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.reject(input), 'RequestRejected');
    }

    sendBack(): void {
        const row = this.selected();
        if (!row || this.acting()) {
            return;
        }

        if (!this.comment?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredToSendBack'));
            return;
        }

        const input = new SendBackRequestInput();
        input.workflowRequestId = row.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.sendBack(input), 'RequestSentBack');
    }

    private act(call: () => any, successKey: string): void {
        this.acting.set(true);

        call()
            .pipe(finalize(() => this.acting.set(false)))
            .subscribe(() => {
                this.notify.success(this.l(successKey));
                this.detail()?.close();
                this.comment = '';
                this.reloadFromServer();
            });
    }

    // ---------------------------------------------------------------- filtering, sorting, paging

    private render(event?: TableLazyLoadEvent): void {
        const filtered = this.allRows().filter((row) => this.matches(row));
        const sorted = this.sort(filtered, event);

        const skip = this.primengTableHelper.getSkipCount(this.paginator(), event);
        const take = this.primengTableHelper.getMaxResultCount(this.paginator(), event);

        this.primengTableHelper.totalRecordsCount.set(sorted.length);
        this.primengTableHelper.records.set(sorted.slice(skip, skip + take));
    }

    private matches(row: EssInboxRow): boolean {
        const text = this.filterText().trim().toLowerCase();
        if (
            text &&
            !`${row.requestNumber} ${row.summary} ${row.employeeName} ${row.typeName}`.toLowerCase().includes(text)
        ) {
            return false;
        }

        if (this.requestTypeFilter() && row.requestType !== this.requestTypeFilter()) {
            return false;
        }

        if (this.employeeFilter() && String(row.employeeId) !== this.employeeFilter()) {
            return false;
        }

        if (this.statusFilter() && row.statusKey !== this.statusFilter()) {
            return false;
        }

        const from = this.dateFrom();
        if (from && (!row.submittedOn || row.submittedOnMs < DateTime.fromISO(from).startOf('day').toMillis())) {
            return false;
        }

        const to = this.dateTo();
        if (to && (!row.submittedOn || row.submittedOnMs > DateTime.fromISO(to).endOf('day').toMillis())) {
            return false;
        }

        return true;
    }

    private sort(rows: EssInboxRow[], event?: TableLazyLoadEvent): EssInboxRow[] {
        const table = this.dataTable();

        const field = (event?.sortField as string) || (table?.sortField as string) || 'submittedOnMs';

        // Oldest first by default: in an approval queue the row that has waited longest is the one that
        // matters, which is also what legacy's "Pending Since" column is for.
        const order = event?.sortOrder ?? table?.sortOrder ?? 1;

        return [...rows].sort((a, b) => {
            const left = a[field as keyof EssInboxRow];
            const right = b[field as keyof EssInboxRow];

            if (left === right) {
                return 0;
            }

            if (typeof left === 'number' && typeof right === 'number') {
                return (left < right ? -1 : 1) * order;
            }

            return String(left ?? '').localeCompare(String(right ?? '')) * order;
        });
    }

    // ---------------------------------------------------------------- mapping

    private toRow(item: EssInboxItemDto): EssInboxRow {
        // Every request the inbox returns is Open and waiting on this person -- that is the query. The
        // status is therefore constant today; it is modelled as a column and a filter anyway so the grid
        // needs no rework when the server starts returning a wider set.
        const statusKey = 'Open';

        return {
            item: item,
            id: item.id,
            requestType: item.requestType ?? '',
            typeName: this.pick(item.requestTypeNameEn, item.requestTypeNameAr) || (item.requestType ?? ''),
            requestNumber: item.requestNumber ?? '',
            employeeId: item.requesterEmployeeId,
            employeeName: this.pick(item.requesterNameEn, item.requesterNameAr),
            photoUrl: item.requesterPhotoUrl,
            summary: this.pick(item.summaryEn, item.summaryAr),
            submittedOn: item.submittedOn,
            submittedOnMs: item.submittedOn ? item.submittedOn.toMillis() : 0,
            stepNumber: item.workflowStep,
            stepLabel: item.totalSteps > 0 ? `${item.workflowStep} / ${item.totalSteps}` : String(item.workflowStep),
            statusKey: statusKey,
            statusLabel: this.l('WorkflowRequestStatus_' + statusKey),
            statusClass: 'badge-warning',
            canReject: item.canReject,
            canSendBack: item.canSendBack,
        };
    }

    /** Bilingual lookup names follow the UI language; the two fields are maintained separately. */
    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }

    private distinct(pairs: { value: string; label: string }[]): { value: string; label: string }[] {
        const seen = new Map<string, string>();

        for (const pair of pairs) {
            if (pair.value && !seen.has(pair.value)) {
                seen.set(pair.value, pair.label || pair.value);
            }
        }

        return [...seen.entries()]
            .map(([value, label]) => ({ value, label }))
            .sort((a, b) => a.label.localeCompare(b.label));
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
