import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AssetsServiceProxy,
    CreateOrEditAssetDto,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    AssetTypesServiceProxy,
    AssetStatus,
    AssetAllocationStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditAssetModal',
    templateUrl: './create-or-edit-asset-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAssetModalComponent extends AppComponentBase {
    private _assetsService = inject(AssetsServiceProxy);
    private _assetTypesService = inject(AssetTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly asset = signal<CreateOrEditAssetDto>(new CreateOrEditAssetDto());
    readonly assetTypes = signal<SimpleLookupDto[]>([]);

    readonly statuses = [
        { value: AssetStatus.Active, label: 'Active' },
        { value: AssetStatus.Inactive, label: 'Inactive' },
    ];

    readonly allocationStatuses = [
        { value: AssetAllocationStatus.Occupied, label: 'Occupied' },
        { value: AssetAllocationStatus.Unoccupied, label: 'Unoccupied' },
    ];

    show(assetId?: number): void {
        this.active.set(true);

        this._assetTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.assetTypes.set(result.items);
            });

        if (!assetId) {
            this.asset.set(new CreateOrEditAssetDto());
            this.modal().show();
        } else {
            this._assetsService.getAssetForEdit(assetId).subscribe((result) => {
                this.asset.set(result.asset);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Asset_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._assetsService
            .createOrEdit(this.asset())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditAssetDto, value: any): void {
        const current = this.asset();
        (current as any)[field] = value;
        const next = new CreateOrEditAssetDto();
        Object.assign(next, current);
        this.asset.set(next);
    }

    updateDateField(field: keyof CreateOrEditAssetDto, value: string): void {
        const current = this.asset();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditAssetDto();
        Object.assign(next, current);
        this.asset.set(next);
    }
}
