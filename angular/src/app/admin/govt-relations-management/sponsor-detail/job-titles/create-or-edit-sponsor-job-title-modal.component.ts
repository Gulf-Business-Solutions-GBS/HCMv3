import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSponsorJobTitleDto,
    GetAllGovtJobTitlesInput,
    GovtJobTitleDto,
    GovtJobTitlesServiceProxy,
    SponsorJobTitlesServiceProxy,
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
    selector: 'createOrEditSponsorJobTitleModal',
    templateUrl: './create-or-edit-sponsor-job-title-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSponsorJobTitleModalComponent extends AppComponentBase {
    private _service = inject(SponsorJobTitlesServiceProxy);
    private _govtJobTitlesService = inject(GovtJobTitlesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSponsorJobTitleDto>(new CreateOrEditSponsorJobTitleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'SponsorJobTitle',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ SponsorId: this.record().sponsorId, GovtJobTitleId: this.record().govtJobTitleId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly govtJobTitles = signal<GovtJobTitleDto[]>([]);

    show(sponsorId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._govtJobTitlesService
            .getAll(new GetAllGovtJobTitlesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, govtJobCategoryId: undefined }))
            .subscribe((result) => this.govtJobTitles.set(result.items));

        if (!id) {
            const record = new CreateOrEditSponsorJobTitleDto();
            record.sponsorId = sponsorId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSponsorJobTitleForEdit(id).subscribe((result) => {
                this.record.set(result.sponsorJobTitle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SponsorJobTitle_GovtJobTitleId')?.focus();
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

    updateField(field: keyof CreateOrEditSponsorJobTitleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSponsorJobTitleDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
