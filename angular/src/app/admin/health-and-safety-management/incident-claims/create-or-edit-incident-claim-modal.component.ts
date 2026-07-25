import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditIncidentClaimDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    GetAllCurrenciesInput,
    GetAllIncidentRegistersInput,
    GetAllSimpleLookupsInput,
    IncidentClaimStatusesServiceProxy,
    IncidentClaimsServiceProxy,
    IncidentRegisterDto,
    IncidentRegistersServiceProxy,
    SimpleLookupDto,
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
    selector: 'createOrEditIncidentClaimModal',
    templateUrl: './create-or-edit-incident-claim-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditIncidentClaimModalComponent extends AppComponentBase {
    private _service = inject(IncidentClaimsServiceProxy);
    private _incidentRegistersService = inject(IncidentRegistersServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);
    private _incidentClaimStatusesService = inject(IncidentClaimStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditIncidentClaimDto>(new CreateOrEditIncidentClaimDto());

    readonly incidentRegisters = signal<IncidentRegisterDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);
    readonly claimStatuses = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditIncidentClaimDto());
            this.modal().show();
        } else {
            this._service.getIncidentClaimForEdit(id).subscribe((result) => {
                this.record.set(result.incidentClaim);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._incidentRegistersService
            .getAll(new GetAllIncidentRegistersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, incidentTypeId: undefined, statusId: undefined }))
            .subscribe((result) => this.incidentRegisters.set(result.items));

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));

        this._incidentClaimStatusesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.claimStatuses.set(result.items));
    }

    onShown(): void {
        document.getElementById('IncidentClaim_ClaimDate')?.focus();
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

    updateField(field: keyof CreateOrEditIncidentClaimDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditIncidentClaimDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'claimDate' | 'paymentDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
