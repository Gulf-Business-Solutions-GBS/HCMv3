import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CountriesServiceProxy,
    CreateOrEditInstitutionDto,
    GetAllSimpleLookupsInput,
    InstitutionsServiceProxy,
    SimpleLookupDto,
    UniversitiesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditInstitutionModal',
    templateUrl: './create-or-edit-institution-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditInstitutionModalComponent extends AppComponentBase {
    private _institutionsService = inject(InstitutionsServiceProxy);
    private _universitiesService = inject(UniversitiesServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly institution = signal<CreateOrEditInstitutionDto>(new CreateOrEditInstitutionDto());
    readonly universities = signal<SimpleLookupDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);

    show(institutionId?: number): void {
        this.active.set(true);

        this._universitiesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.universities.set(result.items);
            });

        this._countriesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.countries.set(result.items));

        if (!institutionId) {
            this.institution.set(new CreateOrEditInstitutionDto());
            this.modal().show();
        } else {
            this._institutionsService.getInstitutionForEdit(institutionId).subscribe((result) => {
                this.institution.set(result.institution);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Institution_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._institutionsService
            .createOrEdit(this.institution())
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
    updateField(field: keyof CreateOrEditInstitutionDto, value: any): void {
        const current = this.institution();
        (current as any)[field] = value;
        const next = new CreateOrEditInstitutionDto();
        Object.assign(next, current);
        this.institution.set(next);
    }
}
