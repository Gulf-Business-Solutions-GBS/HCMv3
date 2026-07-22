import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditNationalityDto,
    GetAllSimpleLookupsInput,
    NationalitiesServiceProxy,
    NationalityGroupsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNationalityModal',
    templateUrl: './create-or-edit-nationality-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditNationalityModalComponent extends AppComponentBase {
    private _nationalitiesService = inject(NationalitiesServiceProxy);
    private _nationalityGroupsService = inject(NationalityGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly nationality = signal<CreateOrEditNationalityDto>(new CreateOrEditNationalityDto());
    readonly nationalityGroups = signal<SimpleLookupDto[]>([]);

    show(nationalityId?: number): void {
        this.active.set(true);

        this._nationalityGroupsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.nationalityGroups.set(result.items);
            });

        if (!nationalityId) {
            this.nationality.set(new CreateOrEditNationalityDto());
            this.modal().show();
        } else {
            this._nationalitiesService.getNationalityForEdit(nationalityId).subscribe((result) => {
                this.nationality.set(result.nationality);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Nationality_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._nationalitiesService
            .createOrEdit(this.nationality())
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
    updateField(field: keyof CreateOrEditNationalityDto, value: any): void {
        const current = this.nationality();
        (current as any)[field] = value;
        const next = new CreateOrEditNationalityDto();
        Object.assign(next, current);
        this.nationality.set(next);
    }
}
