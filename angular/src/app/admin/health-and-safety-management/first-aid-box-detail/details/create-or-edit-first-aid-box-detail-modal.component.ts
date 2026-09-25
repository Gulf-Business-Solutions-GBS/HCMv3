import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditFirstAidBoxDetailDto,
    EmployeeDto,
    EmployeesServiceProxy,
    FirstAidBoxDetailsServiceProxy,
    FirstAidMedicinesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditFirstAidBoxDetailModal',
    templateUrl: './create-or-edit-first-aid-box-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditFirstAidBoxDetailModalComponent extends AppComponentBase {
    private _service = inject(FirstAidBoxDetailsServiceProxy);
    private _medicinesService = inject(FirstAidMedicinesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditFirstAidBoxDetailDto>(new CreateOrEditFirstAidBoxDetailDto());

    readonly medicines = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(firstAidBoxId: number, id?: number): void {
        this.active.set(true);

        this._medicinesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.medicines.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditFirstAidBoxDetailDto();
            record.firstAidBoxId = firstAidBoxId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getFirstAidBoxDetailForEdit(id).subscribe((result) => {
                this.record.set(result.firstAidBoxDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('FirstAidBoxDetail_MedicineId')?.focus();
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

    updateField(field: keyof CreateOrEditFirstAidBoxDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditFirstAidBoxDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'expiryDate' | 'allocationDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
