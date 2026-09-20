import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSponsorGovtRepresentativeDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    SponsorGovtRepresentativesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditSponsorGovtRepresentativeModal',
    templateUrl: './create-or-edit-sponsor-govt-representative-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSponsorGovtRepresentativeModalComponent extends AppComponentBase {
    private _service = inject(SponsorGovtRepresentativesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSponsorGovtRepresentativeDto>(new CreateOrEditSponsorGovtRepresentativeDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'SponsorGovtRepresentative',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ SponsorId: this.record().sponsorId, EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly employees = signal<EmployeeDto[]>([]);

    show(sponsorId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditSponsorGovtRepresentativeDto();
            record.sponsorId = sponsorId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSponsorGovtRepresentativeForEdit(id).subscribe((result) => {
                this.record.set(result.sponsorGovtRepresentative);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SponsorGovtRepresentative_EmployeeId')?.focus();
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

    updateField(field: keyof CreateOrEditSponsorGovtRepresentativeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSponsorGovtRepresentativeDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
