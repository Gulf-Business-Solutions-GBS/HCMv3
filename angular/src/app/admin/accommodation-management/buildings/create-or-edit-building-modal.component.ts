import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BuildingsServiceProxy,
    CreateOrEditBuildingDto,
    GetAllLocationsInput,
    LocationDto,
    LocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditBuildingModal',
    templateUrl: './create-or-edit-building-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditBuildingModalComponent extends AppComponentBase {
    private _service = inject(BuildingsServiceProxy);
    private _locationsService = inject(LocationsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditBuildingDto>(new CreateOrEditBuildingDto());
    readonly locations = signal<LocationDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._locationsService
            .getAll(
                new GetAllLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.locations.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditBuildingDto());
            this.modal().show();
        } else {
            this._service.getBuildingForEdit(id).subscribe((result) => {
                this.record.set(result.building);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Building_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditBuildingDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditBuildingDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
