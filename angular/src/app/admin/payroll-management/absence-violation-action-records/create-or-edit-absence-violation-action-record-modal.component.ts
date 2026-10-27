import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceViolationActionRecordsServiceProxy,
    ActionLogType,
    CreateOrEditAbsenceViolationActionRecordDto,
    LogStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAbsenceViolationActionRecordModal',
    templateUrl: './create-or-edit-absence-violation-action-record-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditAbsenceViolationActionRecordModalComponent extends AppComponentBase {
    private _absenceViolationActionRecordsService = inject(AbsenceViolationActionRecordsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceViolationActionRecord = signal<CreateOrEditAbsenceViolationActionRecordDto>(
        new CreateOrEditAbsenceViolationActionRecordDto()
    );

    readonly actionLogTypes = [
        { value: ActionLogType.Leave, name: 'Leave' },
        { value: ActionLogType.TimeEvent, name: 'TimeEvent' },
    ];

    readonly logStatuses = [
        { value: LogStatus.Open, name: 'Open' },
        { value: LogStatus.Closed, name: 'Closed' },
    ];

    show(absenceViolationActionRecordId?: number): void {
        this.active.set(true);

        if (!absenceViolationActionRecordId) {
            this.absenceViolationActionRecord.set(new CreateOrEditAbsenceViolationActionRecordDto());
            this.modal().show();
        } else {
            this._absenceViolationActionRecordsService
                .getAbsenceViolationActionRecordForEdit(absenceViolationActionRecordId)
                .subscribe((result) => {
                    this.absenceViolationActionRecord.set(result.absenceViolationActionRecord);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceViolationActionRecord_ActionLogType')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceViolationActionRecordsService
            .createOrEdit(this.absenceViolationActionRecord())
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

    updateField(field: keyof CreateOrEditAbsenceViolationActionRecordDto, value: any): void {
        const current = this.absenceViolationActionRecord();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceViolationActionRecordDto();
        Object.assign(next, current);
        this.absenceViolationActionRecord.set(next);
    }
}
