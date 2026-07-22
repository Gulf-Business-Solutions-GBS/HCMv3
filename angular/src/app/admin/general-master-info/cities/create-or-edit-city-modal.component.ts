import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CitiesServiceProxy,
    CreateOrEditCityDto,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    StatesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCityModal',
    templateUrl: './create-or-edit-city-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditCityModalComponent extends AppComponentBase {
    private _citiesService = inject(CitiesServiceProxy);
    private _statesService = inject(StatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly city = signal<CreateOrEditCityDto>(new CreateOrEditCityDto());
    readonly states = signal<SimpleLookupDto[]>([]);

    show(cityId?: number): void {
        this.active.set(true);

        this._statesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.states.set(result.items);
            });

        if (!cityId) {
            this.city.set(new CreateOrEditCityDto());
            this.modal().show();
        } else {
            this._citiesService.getCityForEdit(cityId).subscribe((result) => {
                this.city.set(result.city);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('City_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._citiesService
            .createOrEdit(this.city())
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
    updateField(field: keyof CreateOrEditCityDto, value: any): void {
        const current = this.city();
        (current as any)[field] = value;
        const next = new CreateOrEditCityDto();
        Object.assign(next, current);
        this.city.set(next);
    }
}
