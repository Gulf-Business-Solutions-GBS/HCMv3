import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSocialSecurityRuleDto,
    GetAllSocialSecuritiesInput,
    GetAllSimpleLookupsInput,
    Gender,
    SocialSecurityDto,
    SocialSecuritiesServiceProxy,
    SocialSecurityRulesServiceProxy,
    QualificationsServiceProxy,
    MaritalStatusesServiceProxy,
    SimpleLookupDto,
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

@Component({
    selector: 'createOrEditSocialSecurityRuleModal',
    templateUrl: './create-or-edit-social-security-rule-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSocialSecurityRuleModalComponent extends AppComponentBase {
    private _socialSecurityRulesService = inject(SocialSecurityRulesServiceProxy);
    private _socialSecuritiesService = inject(SocialSecuritiesServiceProxy);
    private _qualificationsService = inject(QualificationsServiceProxy);
    private _maritalStatusesService = inject(MaritalStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly socialSecurityRule = signal<CreateOrEditSocialSecurityRuleDto>(new CreateOrEditSocialSecurityRuleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'SocialSecurityRule',
            record: this.socialSecurityRule,
            isNew: this.isNew,
            keys: () => ({ SocialSecurityId: this.socialSecurityRule().socialSecurityId, QualificationId: this.socialSecurityRule().qualificationId, Gender: this.socialSecurityRule().gender, MaritalStatusId: this.socialSecurityRule().maritalStatusId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly socialSecurities = signal<SocialSecurityDto[]>([]);
    readonly qualifications = signal<SimpleLookupDto[]>([]);
    readonly maritalStatuses = signal<SimpleLookupDto[]>([]);

    readonly genders = [
        { value: Gender.Male, name: 'Male' },
        { value: Gender.Female, name: 'Female' },
    ];

    show(socialSecurityRuleId?: number): void {
        this.isNew.set(!socialSecurityRuleId);
        this.active.set(true);

        this._socialSecuritiesService
            .getAll(new GetAllSocialSecuritiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, nationalityId: undefined }))
            .subscribe((result) => this.socialSecurities.set(result.items));

        this._qualificationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.qualifications.set(result.items));

        this._maritalStatusesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.maritalStatuses.set(result.items));

        if (!socialSecurityRuleId) {
            this.socialSecurityRule.set(new CreateOrEditSocialSecurityRuleDto());
            this.modal().show();
        } else {
            this._socialSecurityRulesService.getSocialSecurityRuleForEdit(socialSecurityRuleId).subscribe((result) => {
                this.socialSecurityRule.set(result.socialSecurityRule);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SocialSecurityRule_SocialSecurityId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._socialSecurityRulesService
            .createOrEdit(this.socialSecurityRule())
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

    updateField(field: keyof CreateOrEditSocialSecurityRuleDto, value: any): void {
        const current = this.socialSecurityRule();
        (current as any)[field] = value;
        const next = new CreateOrEditSocialSecurityRuleDto();
        Object.assign(next, current);
        this.socialSecurityRule.set(next);
    }

    updateDateField(field: keyof CreateOrEditSocialSecurityRuleDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
