import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccommodationServiceTypeDto,
    AccommodationServiceTypesServiceProxy,
    AccommodationServicesServiceProxy,
    CreateOrEditAccommodationServicesDto,
    EmployeeAccommodationDto,
    EmployeeAccommodationsServiceProxy,
    GetAllAccommodationServiceTypesInput,
    GetAllEmployeeAccommodationsInput,
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
    selector: 'createOrEditAccommodationServiceModal',
    templateUrl: './create-or-edit-accommodation-service-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAccommodationServiceModalComponent extends AppComponentBase {
    private _service = inject(AccommodationServicesServiceProxy);
    private _employeeAccommodationsService = inject(EmployeeAccommodationsServiceProxy);
    private _accommodationServiceTypesService = inject(AccommodationServiceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditAccommodationServicesDto>(new CreateOrEditAccommodationServicesDto());
    readonly employeeAccommodations = signal<EmployeeAccommodationDto[]>([]);
    readonly accommodationServiceTypes = signal<AccommodationServiceTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeeAccommodationsService
            .getAll(
                new GetAllEmployeeAccommodationsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.employeeAccommodations.set(result.items));

        this._accommodationServiceTypesService
            .getAll(
                new GetAllAccommodationServiceTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.accommodationServiceTypes.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditAccommodationServicesDto());
            this.modal().show();
        } else {
            this._service.getAccommodationServicesForEdit(id).subscribe((result) => {
                this.record.set(result.accommodationServices);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AccommodationService_EmployeeAccommodationId')?.focus();
    }

    employeeAccommodationLabel(employeeAccommodation: EmployeeAccommodationDto): string {
        return `${employeeAccommodation.employeeCode} - ${employeeAccommodation.employeeFullNameEn}`;
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

    updateField(field: keyof CreateOrEditAccommodationServicesDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditAccommodationServicesDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditAccommodationServicesDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditAccommodationServicesDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
