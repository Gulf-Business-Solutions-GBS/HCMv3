import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPoolCarDto,
    GetAllSimpleLookupsInput,
    PoolCarsServiceProxy,
    SimpleLookupDto,
    VehicleBrandsServiceProxy,
    VehicleModelsServiceProxy,
    VehicleTypesServiceProxy,
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
    selector: 'createOrEditPoolCarModal',
    templateUrl: './create-or-edit-pool-car-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPoolCarModalComponent extends AppComponentBase {
    private _service = inject(PoolCarsServiceProxy);
    private _vehicleTypesService = inject(VehicleTypesServiceProxy);
    private _vehicleBrandsService = inject(VehicleBrandsServiceProxy);
    private _vehicleModelsService = inject(VehicleModelsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPoolCarDto>(new CreateOrEditPoolCarDto());

    readonly vehicleTypes = signal<SimpleLookupDto[]>([]);
    readonly vehicleBrands = signal<SimpleLookupDto[]>([]);
    readonly vehicleModels = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditPoolCarDto());
            this.modal().show();
        } else {
            this._service.getPoolCarForEdit(id).subscribe((result) => {
                this.record.set(result.poolCar);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._vehicleTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.vehicleTypes.set(result.items));

        this._vehicleBrandsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.vehicleBrands.set(result.items));

        this._vehicleModelsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.vehicleModels.set(result.items));
    }

    onShown(): void {
        document.getElementById('PoolCar_VehiclePlateNumber')?.focus();
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

    updateField(field: keyof CreateOrEditPoolCarDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPoolCarDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'vehicleDocExpiryDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
