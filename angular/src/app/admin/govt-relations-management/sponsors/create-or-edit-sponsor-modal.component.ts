import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BusinessTypesServiceProxy,
    CreateOrEditSponsorDto,
    Gender,
    GetAllNationalitiesInput,
    GetAllSimpleLookupsInput,
    GovernoratesServiceProxy,
    NationalitiesServiceProxy,
    NationalityDto,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    SimpleLookupDto,
    SponsorsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSponsorModal',
    templateUrl: './create-or-edit-sponsor-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSponsorModalComponent extends AppComponentBase {
    private _service = inject(SponsorsServiceProxy);
    private _businessTypesService = inject(BusinessTypesServiceProxy);
    private _nationalitiesService = inject(NationalitiesServiceProxy);
    private _governoratesService = inject(GovernoratesServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSponsorDto>(new CreateOrEditSponsorDto());

    readonly businessTypes = signal<SimpleLookupDto[]>([]);
    readonly nationalities = signal<NationalityDto[]>([]);
    readonly governorates = signal<SimpleLookupDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);

    readonly genders = [
        { value: Gender.Male, name: 'Male' },
        { value: Gender.Female, name: 'Female' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditSponsorDto());
            this.modal().show();
        } else {
            this._service.getSponsorForEdit(id).subscribe((result) => {
                this.record.set(result.sponsor);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._businessTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.businessTypes.set(result.items));

        this._nationalitiesService
            .getAll(
                new GetAllNationalitiesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.nationalities.set(result.items));

        this._governoratesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.governorates.set(result.items));

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result) => this.organizationUnits.set(result.items ?? []));
    }

    onShown(): void {
        document.getElementById('Sponsor_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditSponsorDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSponsorDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
