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
    MyAttendanceRequestDto,
    MyAttendanceServiceProxy,
    RequestAttendanceCorrectionInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** Self-service attendance corrections. */
@Component({
    templateUrl: './my-attendance.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyAttendanceComponent extends MyRequestComponentBase<MyAttendanceRequestDto> implements OnInit {
    private readonly _service = inject(MyAttendanceServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly attendanceTypes = signal<SimpleLookupDto[]>([]);

    form = { attendanceTypeId: null as number | null, date: '', startTime: '09:00', endTime: '17:00', comments: '' };

    ngOnInit(): void {
        this._lookups.getAttendanceTypes().subscribe((result) => this.attendanceTypes.set(result ?? []));

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyAttendanceRequestDto[] }> {
        return this._service.getMyAttendances(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyAttendance(input);
    }

    protected resetForm(): void {
        this.form = { attendanceTypeId: null, date: '', startTime: '09:00', endTime: '17:00', comments: '' };
    }

    submit(): void {
        if (!this.form.attendanceTypeId || !this.form.date || this.saving()) {
            return;
        }

        // Required, not merely encouraged: an approver cannot check a claim about a day that has already
        // passed without being told what went wrong.
        if (!this.form.comments?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredForAChange'));
            return;
        }

        const input = new RequestAttendanceCorrectionInput();
        input.attendanceTypeId = this.form.attendanceTypeId;

        // One day per correction. A range would let a single reason cover a fortnight, which is a different
        // and much larger thing to ask an approver to agree to.
        input.startDate = DateTime.fromISO(this.form.date);
        input.endDate = input.startDate;

        input.startTime = DateTime.fromISO(`${this.form.date}T${this.form.startTime}`);
        input.endTime = DateTime.fromISO(`${this.form.date}T${this.form.endTime}`);
        input.attendanceHours = Math.max(0, input.endTime.diff(input.startTime, 'hours').hours);
        input.comments = this.form.comments.trim();

        this.saving.set(true);

        this._service
            .requestAttendanceCorrection(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyAttendanceRequestDto): string {
        return this.pick(row.attendanceTypeNameEn, row.attendanceTypeNameAr);
    }
}
