import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BuildingContractsServiceProxy,
    BuildingDto,
    BuildingsServiceProxy,
    CreateOrEditBuildingContractDto,
    GetAllBuildingsInput,
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
    selector: 'createOrEditBuildingContractModal',
    templateUrl: './create-or-edit-building-contract-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditBuildingContractModalComponent extends AppComponentBase {
    private _service = inject(BuildingContractsServiceProxy);
    private _buildingsService = inject(BuildingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditBuildingContractDto>(new CreateOrEditBuildingContractDto());
    readonly buildings = signal<BuildingDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._buildingsService
            .getAll(new GetAllBuildingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, locationId: undefined }))
            .subscribe((result) => this.buildings.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditBuildingContractDto());
            this.modal().show();
        } else {
            this._service.getBuildingContractForEdit(id).subscribe((result) => {
                this.record.set(result.buildingContract);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('BuildingContract_BuildingId')?.focus();
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

    updateField(field: keyof CreateOrEditBuildingContractDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditBuildingContractDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditBuildingContractDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditBuildingContractDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
