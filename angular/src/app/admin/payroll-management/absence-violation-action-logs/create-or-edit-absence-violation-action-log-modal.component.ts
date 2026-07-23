import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceViolationAction,
    AbsenceViolationActionLogsServiceProxy,
    AbsenceViolationActionRecordDto,
    AbsenceViolationActionRecordsServiceProxy,
    CreateOrEditAbsenceViolationActionLogDto,
    GetAllAbsenceViolationActionRecordsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditAbsenceViolationActionLogModal',
    templateUrl: './create-or-edit-absence-violation-action-log-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceViolationActionLogModalComponent extends AppComponentBase {
    private _absenceViolationActionLogsService = inject(AbsenceViolationActionLogsServiceProxy);
    private _absenceViolationActionRecordsService = inject(AbsenceViolationActionRecordsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceViolationActionLog = signal<CreateOrEditAbsenceViolationActionLogDto>(
        new CreateOrEditAbsenceViolationActionLogDto()
    );
    readonly absenceViolationActionRecords = signal<AbsenceViolationActionRecordDto[]>([]);

    readonly actions = [
        { value: AbsenceViolationAction.Notification, name: 'Notification' },
        { value: AbsenceViolationAction.Violation, name: 'Violation' },
        { value: AbsenceViolationAction.Leave, name: 'Leave' },
    ];

    show(absenceViolationActionLogId?: number): void {
        this.active.set(true);

        this._absenceViolationActionRecordsService
            .getAll(new GetAllAbsenceViolationActionRecordsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.absenceViolationActionRecords.set(result.items));

        if (!absenceViolationActionLogId) {
            this.absenceViolationActionLog.set(new CreateOrEditAbsenceViolationActionLogDto());
            this.modal().show();
        } else {
            this._absenceViolationActionLogsService.getAbsenceViolationActionLogForEdit(absenceViolationActionLogId).subscribe((result) => {
                this.absenceViolationActionLog.set(result.absenceViolationActionLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceViolationActionLog_AbsenceViolationActionRecordId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceViolationActionLogsService
            .createOrEdit(this.absenceViolationActionLog())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    recordLabel(record: AbsenceViolationActionRecordDto): string {
        return `#${record.id} - ${record.actionLogType} - ${record.logStatus}`;
    }

    updateField(field: keyof CreateOrEditAbsenceViolationActionLogDto, value: any): void {
        const current = this.absenceViolationActionLog();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceViolationActionLogDto();
        Object.assign(next, current);
        this.absenceViolationActionLog.set(next);
    }

    updateDateField(field: keyof CreateOrEditAbsenceViolationActionLogDto, value: string): void {
        const current = this.absenceViolationActionLog();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditAbsenceViolationActionLogDto();
        Object.assign(next, current);
        this.absenceViolationActionLog.set(next);
    }
}
