import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    AbsenceTypeDto,
    AmendMyLeaveInput,
    AbsenceTypesServiceProxy,
    ApplyForLeaveInput,
    EntityDto,
    GetAllAbsenceTypesInput,
    GetMyLeavesInput,
    MyLeaveBalanceDto,
    MyLeaveDto,
    MyLeaveServiceProxy,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';

/**
 * Self-service leave: balances, my requests, and applying.
 *
 * <p>The balance card shows Bookable next to Remaining rather than only one number. They differ whenever
 * something is awaiting approval, and an employee who sees only the quota balance will not understand why
 * their application was refused.</p>
 */
@Component({
    templateUrl: './my-leave.component.html',
    styleUrls: ['./my-leave.component.less'],
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLeaveComponent extends AppComponentBase implements OnInit {
    private readonly _myLeaveService = inject(MyLeaveServiceProxy);
    private readonly _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly notLinked = signal(false);

    readonly balances = signal<MyLeaveBalanceDto[]>([]);
    readonly leaves = signal<MyLeaveDto[]>([]);
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    readonly showForm = signal(false);

    /** The approved leave a change is being requested for, or undefined when not amending. */
    readonly amending = signal<MyLeaveDto | undefined>(undefined);

    form = { absenceTypeId: null as number | null, startDate: '', endDate: '', comments: '' };

    amendForm = { absenceTypeId: null as number | null, startDate: '', endDate: '', reason: '' };

    readonly Status = RequestStatus;

    ngOnInit(): void {
        this.loadAbsenceTypes();
        this.reload();
    }

    reload(): void {
        this.loading.set(true);

        this._myLeaveService.getMyBalances().subscribe({
            next: (result) => this.balances.set(result),
            // An unlinked account is a setup gap, not a fault of theirs -- the page explains it rather than
            // showing an empty list or an error toast.
            error: () => this.notLinked.set(true),
        });

        this._myLeaveService
            .getMyLeaves(new GetMyLeavesInput({ includeHistory: false, maxResultCount: 50, skipCount: 0, sorting: undefined }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.leaves.set(result.items),
                error: () => this.notLinked.set(true),
            });
    }

    private loadAbsenceTypes(): void {
        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 200, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));
    }

    openForm(): void {
        this.form = { absenceTypeId: null, startDate: '', endDate: '', comments: '' };
        this.amending.set(undefined);
        this.showForm.set(true);
    }

    closeForm(): void {
        this.showForm.set(false);
    }

    apply(): void {
        if (!this.form.absenceTypeId || !this.form.startDate || !this.form.endDate || this.saving()) {
            return;
        }

        const input = new ApplyForLeaveInput();
        input.absenceTypeId = this.form.absenceTypeId;
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.endDate = DateTime.fromISO(this.form.endDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._myLeaveService
            .applyForLeave(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => {
                // The two outcomes are genuinely different, so say which happened rather than a generic
                // "saved" that implies an approval which may never come.
                this.notify.success(
                    this.l(result.submittedForApproval ? 'LeaveSubmittedForApproval' : 'LeaveRecorded')
                );

                this.closeForm();
                this.reload();
            });
    }

    /**
     * Pre-filled with what the leave says today, so the form is an edit of the current state rather than a
     * blank one the employee has to retype from memory.
     */
    openAmendForm(leave: MyLeaveDto): void {
        this.amendForm = {
            absenceTypeId: leave.absenceTypeId,
            startDate: leave.startDate?.toISODate() ?? '',
            endDate: leave.endDate?.toISODate() ?? '',
            reason: '',
        };

        this.showForm.set(false);
        this.amending.set(leave);
    }

    closeAmendForm(): void {
        this.amending.set(undefined);
    }

    submitAmendment(): void {
        const target = this.amending();
        if (!target || this.saving()) {
            return;
        }

        if (!this.amendForm.absenceTypeId || !this.amendForm.startDate || !this.amendForm.endDate) {
            return;
        }

        // Required, not merely encouraged. An approver deciding on a change to something they already
        // granted needs to know what prompted it.
        if (!this.amendForm.reason?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredForAChange'));
            return;
        }

        const input = new AmendMyLeaveInput();
        input.employeeAbsenceId = target.id;
        input.absenceTypeId = this.amendForm.absenceTypeId;
        input.startDate = DateTime.fromISO(this.amendForm.startDate);
        input.endDate = DateTime.fromISO(this.amendForm.endDate);
        input.reason = this.amendForm.reason.trim();

        this.saving.set(true);

        this._myLeaveService
            .amendMyLeave(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('AmendmentSubmittedForApproval'));
                this.closeAmendForm();
                this.reload();
            });
    }

    cancel(leave: MyLeaveDto): void {
        this.message.confirm(this.l('LeaveCancelWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._myLeaveService.cancelMyLeave(new EntityDto({ id: leave.id })).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.reload();
            });
        });
    }

    typeName(balance: MyLeaveBalanceDto): string {
        return this.isRtl ? balance.absenceTypeNameAr || balance.absenceTypeNameEn : balance.absenceTypeNameEn;
    }

    leaveTypeName(leave: MyLeaveDto): string {
        return this.isRtl ? leave.absenceTypeNameAr || leave.absenceTypeNameEn : leave.absenceTypeNameEn;
    }

    /**
     * A row with no workflow status was entered by HR rather than requested, so it is shown as simply
     * recorded rather than given a status badge that would imply it went through approval.
     */
    statusLabel(leave: MyLeaveDto): string {
        if (leave.workflowStatus === undefined || leave.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[leave.workflowStatus]);
    }

    statusClass(leave: MyLeaveDto): string {
        switch (leave.workflowStatus) {
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

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
