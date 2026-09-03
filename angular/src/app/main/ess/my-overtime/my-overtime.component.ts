import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyOvertimeRequestDto,
    MyOvertimeServiceProxy,
    RequestOvertimeInput,
    RequestStatus,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** Self-service overtime claims. */
@Component({
    templateUrl: './my-overtime.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyOvertimeComponent extends MyRequestComponentBase<MyOvertimeRequestDto> implements OnInit {
    private readonly _service = inject(MyOvertimeServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly overtimeTypes = signal<SimpleLookupDto[]>([]);

    readonly Status = RequestStatus;

    form = { overtimeTypeId: null as number | null, effectiveDate: '', overtimeHours: null as number | null, comments: '' };

    ngOnInit(): void {
        this._lookups.getOvertimeTypes().subscribe((result) => this.overtimeTypes.set(result ?? []));

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyOvertimeRequestDto[] }> {
        return this._service.getMyOvertimes(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyOvertime(input);
    }

    protected resetForm(): void {
        this.form = { overtimeTypeId: null, effectiveDate: '', overtimeHours: null, comments: '' };
    }

    submit(): void {
        if (!this.form.overtimeTypeId || !this.form.effectiveDate || this.saving()) {
            return;
        }

        if (!this.form.overtimeHours || this.form.overtimeHours <= 0) {
            this.message.warn(this.l('OvertimeHoursMustBeGreaterThanZero'));
            return;
        }

        if (this.form.overtimeHours > 24) {
            this.message.warn(this.l('OvertimeHoursCannotExceedTwentyFourInADay'));
            return;
        }

        const input = new RequestOvertimeInput();
        input.overtimeTypeId = this.form.overtimeTypeId;
        input.effectiveDate = DateTime.fromISO(this.form.effectiveDate);
        input.overtimeHours = this.form.overtimeHours;
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestOvertime(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyOvertimeRequestDto): string {
        return this.pick(row.overtimeTypeNameEn, row.overtimeTypeNameAr);
    }

    /**
     * Approved hours are only meaningful once somebody has approved them. Before that the field is zero,
     * and showing "0" next to a claim for 5 would read as a decision rather than an absence of one.
     */
    showsApprovedHours(row: MyOvertimeRequestDto): boolean {
        return row.workflowStatus === RequestStatus.Approved || row.workflowStatus === RequestStatus.Closed;
    }
}
