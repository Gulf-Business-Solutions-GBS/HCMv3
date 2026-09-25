import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeFundExemptionDto,
    EmployeeFundDto,
    EmployeeFundExemptionsServiceProxy,
    EmployeeFundsServiceProxy,
    GetAllEmployeeFundsInput,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeFundExemptionModal',
    templateUrl: './create-or-edit-employee-fund-exemption-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CreateOrEditEmployeeFundExemptionModalComponent extends AppComponentBase {
    private _service = inject(EmployeeFundExemptionsServiceProxy);
    private _employeeFundsService = inject(EmployeeFundsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeFundExemptionDto>(new CreateOrEditEmployeeFundExemptionDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeFundExemption',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeFundId: this.record().employeeFundId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly employeeFunds = signal<EmployeeFundDto[]>([]);

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._employeeFundsService
            .getAll(new GetAllEmployeeFundsInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeFunds.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditEmployeeFundExemptionDto());
            this.modal().show();
        } else {
            this._service.getEmployeeFundExemptionForEdit(id).subscribe((result) => {
                this.record.set(result.employeeFundExemption);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeFundExemption_EmployeeFundId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeFundExemptionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeFundExemptionDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeFundExemptionDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
