import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDocumentMvmtDto,
    EmployeeDocumentMvmtDto,
    EmployeeDocumentMvmtsServiceProxy,
    GetAllEmployeeDocumentMvmtsInput,
    RecordDocumentHandOverInput,
    RecordDocumentReturnInput,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';

@Component({
    selector: 'employee-document-movements-modal',
    templateUrl: './employee-document-movements-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
        PermissionPipe,
    ],
})
export class EmployeeDocumentMovementsModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDocumentMvmtsServiceProxy);

    /** Recording a hand-over or a return is an edit of the movement, so both sit under its Edit permission. */
    private readonly editPermission = 'Pages.PersonnelAdministration.EmployeeDocumentMvmts.Edit';

    readonly modal = viewChild<ModalDirective>('movementsModal');

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly employeeDocumentId = signal<number | undefined>(undefined);
    readonly movements = signal<EmployeeDocumentMvmtDto[]>([]);
    readonly newRecord = signal<CreateOrEditEmployeeDocumentMvmtDto>(new CreateOrEditEmployeeDocumentMvmtDto());

    /** The movement whose hand-over is being recorded, if any. Null means that form is closed. */
    readonly handOverRow = signal<EmployeeDocumentMvmtDto | null>(null);
    readonly handOverDate = signal<string>('');

    /** The movement whose return is being recorded, if any. Null means that form is closed. */
    readonly returnRow = signal<EmployeeDocumentMvmtDto | null>(null);
    readonly returnDate = signal<string>('');
    readonly returnRemarks = signal<string>('');

    /** True while a hand-over or a return is being sent. */
    readonly recording = signal<boolean>(false);

    /** The movement whose earlier due dates are listed under its row, if any. */
    readonly expandedId = signal<number | undefined>(undefined);

    show(employeeDocumentId: number): void {
        this.active.set(true);
        this.employeeDocumentId.set(employeeDocumentId);
        this.resetNewRecord();
        this.closeRecordForms();
        this.expandedId.set(undefined);
        this.loadMovements();
        this.modal().show();
    }

    private loadMovements(): void {
        if (!this.employeeDocumentId()) {
            return;
        }

        this.loading.set(true);
        this._service
            .getAll(
                new GetAllEmployeeDocumentMvmtsInput({
                    employeeDocumentId: this.employeeDocumentId(),
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.movements.set(result.items));
    }

    private resetNewRecord(): void {
        const record = new CreateOrEditEmployeeDocumentMvmtDto();
        record.employeeDocumentId = this.employeeDocumentId();
        this.newRecord.set(record);
    }

    updateField(field: keyof CreateOrEditEmployeeDocumentMvmtDto, value: any): void {
        const current = this.newRecord();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDocumentMvmtDto();
        Object.assign(next, current);
        this.newRecord.set(next);
    }

    updateDateField(field: 'withdrawDate' | 'returnDate' | 'actualReturnDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    addMovement(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.newRecord())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.resetNewRecord();
                this.loadMovements();
            });
    }

    deleteMovement(record: EmployeeDocumentMvmtDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.loadMovements();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    /**
     * A movement HR entered has no workflow status, so it reads as simply recorded rather than being given a
     * status that implies it went through approval.
     */
    statusLabel(record: EmployeeDocumentMvmtDto): string {
        if (record.workflowStatus === undefined || record.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[record.workflowStatus]);
    }

    /** Bilingual names follow the UI language; the two fields are maintained separately. */
    guarantorName(record: EmployeeDocumentMvmtDto): string {
        const isRtl = document?.documentElement?.getAttribute('dir') === 'rtl';

        return (isRtl ? record.guarantorNameAr || record.guarantorNameEn : record.guarantorNameEn) ?? '';
    }

    /**
     * True when the document can be handed over or taken back on this movement: the user may edit movements,
     * the movement is in effect (entered by HR, approved or closed -- not a request still being decided or one
     * that was refused), and the document has not already come back.
     */
    canRecord(record: EmployeeDocumentMvmtDto): boolean {
        if (!this.isGranted(this.editPermission) || record.actualReturnDate) {
            return false;
        }

        const status = record.workflowStatus;

        return status === undefined || status === null || status === RequestStatus.Approved || status === RequestStatus.Closed;
    }

    /** Today as the date fields hold it: neither a hand-over nor a return can be dated in the future. */
    todayIso(): string {
        return DateTime.local().toISODate() ?? '';
    }

    /** The earliest day the document can have come back: the day it was handed over, else the day it was due out. */
    returnMinIso(record: EmployeeDocumentMvmtDto): string | null {
        return (record.actualWithdrawDate ?? record.withdrawDate)?.toISODate() ?? null;
    }

    openHandOver(record: EmployeeDocumentMvmtDto): void {
        this.closeRecordForms();

        // A correction opens on the day already recorded; a first recording opens on today.
        this.handOverDate.set(record.actualWithdrawDate?.toISODate() ?? this.todayIso());
        this.handOverRow.set(record);
    }

    openReturn(record: EmployeeDocumentMvmtDto): void {
        this.closeRecordForms();
        this.returnDate.set(this.todayIso());
        this.returnRemarks.set('');
        this.returnRow.set(record);
    }

    closeRecordForms(): void {
        this.handOverRow.set(null);
        this.returnRow.set(null);
    }

    submitHandOver(): void {
        const record = this.handOverRow();

        if (!record || !this.handOverDate() || this.recording()) {
            return;
        }

        const input = new RecordDocumentHandOverInput();
        input.id = record.id;
        input.actualWithdrawDate = DateTime.fromISO(this.handOverDate());

        this.recording.set(true);
        this._service
            .recordHandOver(input)
            .pipe(finalize(() => this.recording.set(false)))
            .subscribe(() => this.afterRecorded());
    }

    submitReturn(): void {
        const record = this.returnRow();

        if (!record || !this.returnDate() || this.recording()) {
            return;
        }

        const input = new RecordDocumentReturnInput();
        input.id = record.id;
        input.actualReturnDate = DateTime.fromISO(this.returnDate());
        input.returnRemarks = this.returnRemarks()?.trim() || undefined;

        this.recording.set(true);
        this._service
            .recordReturn(input)
            .pipe(finalize(() => this.recording.set(false)))
            .subscribe(() => this.afterRecorded());
    }

    private afterRecorded(): void {
        this.notify.success(this.l('SavedSuccessfully'));
        this.closeRecordForms();
        this.loadMovements();
    }

    toggleExtensions(record: EmployeeDocumentMvmtDto): void {
        this.expandedId.set(this.expandedId() === record.id ? undefined : record.id);
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
