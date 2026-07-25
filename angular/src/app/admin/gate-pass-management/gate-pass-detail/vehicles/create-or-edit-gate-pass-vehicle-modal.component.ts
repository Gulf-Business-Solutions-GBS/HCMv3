import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditGatePassVehicleDto,
    GatePassVehiclesServiceProxy,
    GetAllSimpleLookupsInput,
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

@Component({
    selector: 'createOrEditGatePassVehicleModal',
    templateUrl: './create-or-edit-gate-pass-vehicle-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditGatePassVehicleModalComponent extends AppComponentBase {
    private _service = inject(GatePassVehiclesServiceProxy);
    private _vehicleTypesService = inject(VehicleTypesServiceProxy);
    private _vehicleBrandsService = inject(VehicleBrandsServiceProxy);
    private _vehicleModelsService = inject(VehicleModelsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditGatePassVehicleDto>(new CreateOrEditGatePassVehicleDto());

    readonly vehicleTypes = signal<SimpleLookupDto[]>([]);
    readonly vehicleBrands = signal<SimpleLookupDto[]>([]);
    readonly vehicleModels = signal<SimpleLookupDto[]>([]);

    show(gatePassId: number, id?: number): void {
        this.active.set(true);

        this._vehicleTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.vehicleTypes.set(result.items));

        this._vehicleBrandsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.vehicleBrands.set(result.items));

        this._vehicleModelsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.vehicleModels.set(result.items));

        if (!id) {
            const record = new CreateOrEditGatePassVehicleDto();
            record.gatePassId = gatePassId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getGatePassVehicleForEdit(id).subscribe((result) => {
                this.record.set(result.gatePassVehicle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('GatePassVehicle_VehicleTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditGatePassVehicleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditGatePassVehicleDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'vehicleDocExpiryDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
