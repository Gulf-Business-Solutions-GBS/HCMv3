import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AssetTypesServiceProxy,
    CreateOrEditAssetTypeDto,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAssetTypeModal',
    templateUrl: './create-or-edit-asset-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditAssetTypeModalComponent extends AppComponentBase {
    private _assetTypesService = inject(AssetTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly assetType = signal<CreateOrEditAssetTypeDto>(new CreateOrEditAssetTypeDto());
    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(assetTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: 'Code ASC', filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!assetTypeId) {
            const assetType = new CreateOrEditAssetTypeDto();
            // Legacy default: a new asset type is not offered in self-service until switched on.
            assetType.includeInWorkflow = false;
            this.assetType.set(assetType);
            this.modal().show();
        } else {
            this._assetTypesService.getAssetTypeForEdit(assetTypeId).subscribe((result) => {
                this.assetType.set(result.assetType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AssetType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._assetTypesService
            .createOrEdit(this.assetType())
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
    updateField(field: keyof CreateOrEditAssetTypeDto, value: any): void {
        const current = this.assetType();
        (current as any)[field] = value;
        const next = new CreateOrEditAssetTypeDto();
        Object.assign(next, current);
        this.assetType.set(next);
    }
}
