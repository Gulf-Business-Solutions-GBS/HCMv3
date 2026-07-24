import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeRemunerationDto,
    EmployeeRemunerationsServiceProxy,
    GetAllWageTypesInput,
    PaymentUnit,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeRemunerationModal',
    templateUrl: './create-or-edit-employee-remuneration-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeRemunerationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeRemunerationsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeRemunerationDto>(new CreateOrEditEmployeeRemunerationDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);

    readonly paymentUnits = [
        { value: PaymentUnit.Days, name: 'Days' },
        { value: PaymentUnit.Hours, name: 'Hours' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeRemunerationDto();
            record.employeeId = employeeId;
            record.paymentNumber = 0;
            record.paymentUnit = PaymentUnit.Days;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeRemunerationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeRemuneration);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeRemuneration_WageTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeRemunerationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeRemunerationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeRemunerationDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeRemunerationDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
