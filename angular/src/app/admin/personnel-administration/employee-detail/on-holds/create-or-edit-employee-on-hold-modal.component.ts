import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeOnHoldDto,
    EmployeeActionDto,
    EmployeeActionsServiceProxy,
    EmployeeOnHoldsServiceProxy,
    GetAllEmployeeActionsInput,
    OnHoldStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditEmployeeOnHoldModal',
    templateUrl: './create-or-edit-employee-on-hold-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe, LuxonFormatPipe],
})
export class CreateOrEditEmployeeOnHoldModalComponent extends AppComponentBase {
    private _service = inject(EmployeeOnHoldsServiceProxy);
    private _employeeActionsService = inject(EmployeeActionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeOnHoldDto>(new CreateOrEditEmployeeOnHoldDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeOnHold',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly employeeActions = signal<EmployeeActionDto[]>([]);

    readonly bankTransferOptions = [
        { value: OnHoldStatus.NA, name: 'NA' },
        { value: OnHoldStatus.Hold, name: 'Hold' },
        { value: OnHoldStatus.Unhold, name: 'Unhold' },
    ];

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._employeeActionsService
            .getAll(new GetAllEmployeeActionsInput({ employeeId, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeActions.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeOnHoldDto();
            record.employeeId = employeeId;
            record.bankTransfer = OnHoldStatus.NA;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeOnHoldForEdit(id).subscribe((result) => {
                this.record.set(result.employeeOnHold);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeOnHold_StartDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditEmployeeOnHoldDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeOnHoldDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeOnHoldDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
