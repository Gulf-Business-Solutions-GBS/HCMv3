import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSponsorLicenseInfoDto,
    SponsorLicenseInfosServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSponsorLicenseInfoModal',
    templateUrl: './create-or-edit-sponsor-license-info-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSponsorLicenseInfoModalComponent extends AppComponentBase {
    private _service = inject(SponsorLicenseInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSponsorLicenseInfoDto>(new CreateOrEditSponsorLicenseInfoDto());

    show(sponsorId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditSponsorLicenseInfoDto();
            record.sponsorId = sponsorId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSponsorLicenseInfoForEdit(id).subscribe((result) => {
                this.record.set(result.sponsorLicenseInfo);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SponsorLicenseInfo_TradeLicenseNum')?.focus();
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

    updateField(field: keyof CreateOrEditSponsorLicenseInfoDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSponsorLicenseInfoDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'licenseIssueDate' | 'licenseExpiryDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
