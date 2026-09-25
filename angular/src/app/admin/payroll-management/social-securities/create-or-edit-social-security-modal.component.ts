import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSocialSecurityDto,
    GetAllNationalitiesInput,
    NationalityDto,
    NationalitiesServiceProxy,
    SocialSecuritiesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSocialSecurityModal',
    templateUrl: './create-or-edit-social-security-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSocialSecurityModalComponent extends AppComponentBase {
    private _socialSecuritiesService = inject(SocialSecuritiesServiceProxy);
    private _nationalitiesService = inject(NationalitiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly socialSecurity = signal<CreateOrEditSocialSecurityDto>(new CreateOrEditSocialSecurityDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'SocialSecurity',
            record: this.socialSecurity,
            isNew: this.isNew,
            keys: () => ({ NationalityId: this.socialSecurity().nationalityId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly nationalities = signal<NationalityDto[]>([]);

    show(socialSecurityId?: number): void {
        this.isNew.set(!socialSecurityId);
        this.active.set(true);

        this._nationalitiesService
            .getAll(new GetAllNationalitiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.nationalities.set(result.items));

        if (!socialSecurityId) {
            this.socialSecurity.set(new CreateOrEditSocialSecurityDto());
            this.modal().show();
        } else {
            this._socialSecuritiesService.getSocialSecurityForEdit(socialSecurityId).subscribe((result) => {
                this.socialSecurity.set(result.socialSecurity);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SocialSecurity_NationalityId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._socialSecuritiesService
            .createOrEdit(this.socialSecurity())
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

    updateField(field: keyof CreateOrEditSocialSecurityDto, value: any): void {
        const current = this.socialSecurity();
        (current as any)[field] = value;
        const next = new CreateOrEditSocialSecurityDto();
        Object.assign(next, current);
        this.socialSecurity.set(next);
    }

    updateDateField(field: keyof CreateOrEditSocialSecurityDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
