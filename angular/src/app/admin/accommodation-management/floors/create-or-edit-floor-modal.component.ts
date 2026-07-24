import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BuildingDto,
    BuildingsServiceProxy,
    CreateOrEditFloorDto,
    FloorsServiceProxy,
    GetAllBuildingsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditFloorModal',
    templateUrl: './create-or-edit-floor-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditFloorModalComponent extends AppComponentBase {
    private _service = inject(FloorsServiceProxy);
    private _buildingsService = inject(BuildingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditFloorDto>(new CreateOrEditFloorDto());
    readonly buildings = signal<BuildingDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._buildingsService
            .getAll(new GetAllBuildingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, locationId: undefined }))
            .subscribe((result) => this.buildings.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditFloorDto());
            this.modal().show();
        } else {
            this._service.getFloorForEdit(id).subscribe((result) => {
                this.record.set(result.floor);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Floor_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditFloorDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditFloorDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
