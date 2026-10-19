import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EntityDto,
    GetLeavePlannerInput,
    GetMyRequestsInput,
    LeavePlannerDto,
    MyLeavePlanDto,
    MyLeavePlanServiceProxy,
    RequestLeavePlanInput,
    SaveLeavePlanDraftInput,
} from '@shared/service-proxies/service-proxies';
import { MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { DateTime } from 'luxon';
import { Observable, firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { LeavePlannerGridComponent } from '../../../shared/common/leave-planner/leave-planner-grid.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { LeavePlanFormComponent, LeavePlanFormValue, emptyLeavePlanForm } from './leave-plan-form.component';

/**
 * Self-service leave plans (legacy ESS <c>LeavePlannerController</c>, "Leave Planner Request").
 *
 * <p>Laid out like every other self-service request, the exit permit being the model: the list, Details in the
 * shared dialog, the form as its own component with Save, Submit and Cancel. Legacy's page also had a "Leave
 * Scheduler" button beside the create button, opening the month view of the employee's own and their direct
 * reports' plans and leave; it is kept, as a card shown under the header.</p>
 *
 * <p>A plan books nothing. The leave itself is still applied for on the leave screen.</p>
 */
@Component({
    templateUrl: './my-leave-plan.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        SubHeaderComponent,
        LocalizePipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        LeavePlanFormComponent,
        LeavePlannerGridComponent,
        TieredMenu,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLeavePlanComponent extends MyRequestComponentBase<MyLeavePlanDto> implements OnInit {
    private readonly _service = inject(MyLeavePlanServiceProxy);

    /** The list's columns -- legacy's ESS list: request number, start, end, planned days, created, status. */
    readonly list = new EssList<MyLeavePlanDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,
            sort: (row) => (row.requestNumber ? Number(row.requestNumber) || row.requestNumber : row.isDraft ? Number.MAX_SAFE_INTEGER : 0),
        },
        { key: 'startDate', label: 'StartDate', text: (row) => this.detailDate(row.startDate), sort: (row) => row.startDate?.toMillis() },
        { key: 'endDate', label: 'EndDate', text: (row) => this.detailDate(row.endDate), sort: (row) => row.endDate?.toMillis() },
        { key: 'plannedDays', label: 'PlannedDays', text: (row) => (row.plannedDays > 0 ? row.plannedDays : ''), sort: (row) => row.plannedDays, align: 'end' },
        { key: 'createdDate', label: 'CreatedDate', text: (row) => this.detailDate(row.creationTime), sort: (row) => row.creationTime?.toMillis() },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    /** The Actions menu's entries, for whichever row's button was pressed last. */
    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** The create form's data, owned here and written into by the form component through ngModel. */
    form: LeavePlanFormValue = emptyLeavePlanForm();

    /** Legacy "Leave Scheduler": shown or not, the month it is on, and what it holds. */
    readonly showScheduler = signal(false);
    readonly schedulerMonth = signal(DateTime.local().startOf('month'));
    readonly scheduler = signal<LeavePlannerDto | undefined>(undefined);
    readonly schedulerLoading = signal(false);

    ngOnInit(): void {
        this.reload();

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already explained by
        // the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);

                // A NEW object: the form component is OnPush and takes this as an input.
                this.form = { ...this.form, employeeDisplay: this.employeeDisplay() };
            },
            error: () => undefined,
        });
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyLeavePlanDto[] }> {
        return this._service.getMyLeavePlans(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyLeavePlan(input);
    }

    protected resetForm(): void {
        this.form = emptyLeavePlanForm(this.employeeDisplay());
    }

    protected requestType(): string {
        return 'LeavePlan';
    }

    protected detailTitle(): string {
        return this.l('EssMyLeavePlans');
    }

    protected detailFields(row: MyLeavePlanDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('StartDate'), value: this.detailDate(row.startDate) },
            { label: this.l('EndDate'), value: this.detailDate(row.endDate) },
            { label: this.l('PlannedDays'), value: row.plannedDays > 0 ? `${row.plannedDays}` : '' },
            {
                label: this.l('ProjectedLeaveBalance'),
                value: row.projectedLeaveBalance === undefined || row.projectedLeaveBalance === null ? '' : `${row.projectedLeaveBalance}`,
            },
        ];
    }

    showMenu(event: Event, row: MyLeavePlanDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.menuItems(row));
        menu.toggle(event);
    }

    /** Every action on every row, in the same order; an inapplicable one is greyed out, never hidden. */
    private menuItems(row: MyLeavePlanDto): MenuItem[] {
        return [
            { label: this.l('Edit'), disabled: !row.canEdit, command: () => this.continueDraft(row) },
            { label: this.l('Details'), disabled: row.isDraft, command: () => this.openDetail(row) },
            // A draft was never sent to anybody, so it is discarded rather than withdrawn.
            { label: this.l(row.isDraft ? 'DiscardDraft' : 'WithdrawRequest'), disabled: !row.canCancel, command: () => this.cancel(row) },
        ];
    }

    /** Save: files what is on the form as a draft. Nothing is checked -- that happens at Submit. */
    save(): void {
        if (this.saving()) {
            return;
        }

        this.saving.set(true);

        this._service
            .saveMyLeavePlanDraft(this.draftInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => this.notifyDraftSaved());
    }

    /**
     * Submit: a saved draft or a plan sent back is saved and then submitted, so what the approver sees is what is
     * on the screen; a new one is created and submitted in one call.
     */
    submit(): void {
        if (this.saving()) {
            return;
        }

        // Legacy LeavePlannerController.ValidateSubmitOrSave, said before the round trip. The server says it too.
        if (!this.form.startDate) {
            this.message.warn(this.l('StartDateIsRequired'));
            return;
        }

        if (!this.form.endDate) {
            this.message.warn(this.l('EndDateIsRequired'));
            return;
        }

        if (this.form.endDate < this.form.startDate) {
            this.message.warn(this.l('EndDateCannotBeEarlierThanStartDate'));
            return;
        }

        const draftId = this.editingId();

        void this.run(async () => {
            if (draftId !== undefined) {
                await firstValueFrom(this._service.saveMyLeavePlanDraft(this.draftInput()));
                const result = await firstValueFrom(this._service.submitMyLeavePlanDraft(new EntityDto({ id: draftId })));
                this.notifySubmitted(result.submittedForApproval);
                return;
            }

            const input = new RequestLeavePlanInput();
            input.startDate = DateTime.fromISO(this.form.startDate);
            input.endDate = DateTime.fromISO(this.form.endDate);
            input.comments = this.form.comments;

            const result = await firstValueFrom(this._service.requestLeavePlan(input));
            this.notifySubmitted(result.submittedForApproval);
        });
    }

    /** Reopens a saved draft, or a plan an approver sent back, in the same form it was filled in on. */
    continueDraft(row: MyLeavePlanDto): void {
        // Raw id: NSwag flattens EntityDto on GET.
        this._service.getMyLeavePlanDraftForEdit(row.id).subscribe((draft) => {
            this.form = {
                employeeDisplay: this.employeeDisplay(),
                startDate: this.toInputDate(draft.startDate),
                endDate: this.toInputDate(draft.endDate),
                comments: draft.comments ?? '',
            };

            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    // ------------------------------------------------------------------ Leave Scheduler

    toggleScheduler(): void {
        const show = !this.showScheduler();
        this.showScheduler.set(show);

        if (show && !this.scheduler()) {
            this.loadScheduler();
        }
    }

    moveSchedulerMonth(months: number): void {
        this.schedulerMonth.set(this.schedulerMonth().plus({ months }));
        this.loadScheduler();
    }

    private loadScheduler(): void {
        this.schedulerLoading.set(true);

        this._service
            .getMyTeamLeavePlanner(
                new GetLeavePlannerInput({
                    // Mid-month, so no time-zone conversion on the way can move it into the next or previous month.
                    month: this.schedulerMonth().plus({ days: 14 }),
                    organizationUnitId: undefined,
                    filter: undefined,
                    // Legacy's ESS scheduler showed every plan and every leave of the team, whatever its state.
                    // Pending is kept, rejected and drafts are not (see LeavePlannerReader).
                    includePlanned: true,
                    includePlannedPending: true,
                    includeLeave: true,
                    includeLeavePending: true,
                })
            )
            .pipe(finalize(() => this.schedulerLoading.set(false)))
            .subscribe((result) => this.scheduler.set(result));
    }

    private draftInput(): SaveLeavePlanDraftInput {
        const input = new SaveLeavePlanDraftInput();
        input.id = this.editingId();
        input.startDate = this.form.startDate ? DateTime.fromISO(this.form.startDate) : undefined;
        input.endDate = this.form.endDate ? DateTime.fromISO(this.form.endDate) : undefined;
        input.comments = this.form.comments;

        return input;
    }

    /** Runs one submit with the buttons disabled throughout. A refused call has already been explained. */
    private async run(work: () => Promise<void>): Promise<void> {
        this.saving.set(true);

        try {
            await work();
        } catch {
            // Already reported to the employee.
        } finally {
            this.saving.set(false);
        }
    }

    /** ISO yyyy-MM-dd for a date input; empty for an unset date rather than the year 1. */
    private toInputDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }
}
