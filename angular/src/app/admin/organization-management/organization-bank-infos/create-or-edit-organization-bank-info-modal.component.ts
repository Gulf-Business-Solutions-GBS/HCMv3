import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BanksServiceProxy,
    CreateOrEditOrganizationBankInfoDto,
    GetAllSimpleLookupsInput,
    OrganizationBankInfosServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
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
    selector: 'createOrEditOrganizationBankInfoModal',
    templateUrl: './create-or-edit-organization-bank-info-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditOrganizationBankInfoModalComponent extends AppComponentBase {
    private _organizationBankInfosService = inject(OrganizationBankInfosServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _banksService = inject(BanksServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly organizationBankInfo = signal<CreateOrEditOrganizationBankInfoDto>(new CreateOrEditOrganizationBankInfoDto());
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly banks = signal<SimpleLookupDto[]>([]);

    show(organizationBankInfoId?: number): void {
        this.active.set(true);

        this._organizationUnitService.getAll().subscribe((result) => {
            this.organizationUnits.set(result);
        });

        this._banksService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.banks.set(result.items);
            });

        if (!organizationBankInfoId) {
            this.organizationBankInfo.set(new CreateOrEditOrganizationBankInfoDto());
            this.modal().show();
        } else {
            this._organizationBankInfosService.getOrganizationBankInfoForEdit(organizationBankInfoId).subscribe((result) => {
                this.organizationBankInfo.set(result.organizationBankInfo);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OrganizationBankInfo_OrganizationUnitId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._organizationBankInfosService
            .createOrEdit(this.organizationBankInfo())
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

    updateField(field: keyof CreateOrEditOrganizationBankInfoDto, value: any): void {
        const current = this.organizationBankInfo();
        (current as any)[field] = value;
        const next = new CreateOrEditOrganizationBankInfoDto();
        Object.assign(next, current);
        this.organizationBankInfo.set(next);
    }
}
