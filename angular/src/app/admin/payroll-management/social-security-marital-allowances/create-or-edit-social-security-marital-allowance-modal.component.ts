import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSocialSecurityMaritalAllowanceDto,
    GetAllSocialSecuritiesInput,
    GetAllSimpleLookupsInput,
    SocialSecurityDto,
    SocialSecuritiesServiceProxy,
    SocialSecurityMaritalAllowancesServiceProxy,
    EducationBranchesServiceProxy,
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

@Component({
    selector: 'createOrEditSocialSecurityMaritalAllowanceModal',
    templateUrl: './create-or-edit-social-security-marital-allowance-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSocialSecurityMaritalAllowanceModalComponent extends AppComponentBase {
    private _socialSecurityMaritalAllowancesService = inject(SocialSecurityMaritalAllowancesServiceProxy);
    private _socialSecuritiesService = inject(SocialSecuritiesServiceProxy);
    private _educationBranchesService = inject(EducationBranchesServiceProxy);
    private _maritalStatusesService = inject(MaritalStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly socialSecurityMaritalAllowance = signal<CreateOrEditSocialSecurityMaritalAllowanceDto>(
        new CreateOrEditSocialSecurityMaritalAllowanceDto()
    );
    readonly socialSecurities = signal<SocialSecurityDto[]>([]);
    readonly educationBranches = signal<SimpleLookupDto[]>([]);
    readonly maritalStatuses = signal<SimpleLookupDto[]>([]);

    show(socialSecurityMaritalAllowanceId?: number): void {
        this.active.set(true);

        this._socialSecuritiesService
            .getAll(
                new GetAllSocialSecuritiesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    nationalityId: undefined,
                })
            )
            .subscribe((result) => this.socialSecurities.set(result.items));

        this._educationBranchesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.educationBranches.set(result.items));

        this._maritalStatusesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.maritalStatuses.set(result.items));

        if (!socialSecurityMaritalAllowanceId) {
            this.socialSecurityMaritalAllowance.set(new CreateOrEditSocialSecurityMaritalAllowanceDto());
            this.modal().show();
        } else {
            this._socialSecurityMaritalAllowancesService
                .getSocialSecurityMaritalAllowanceForEdit(socialSecurityMaritalAllowanceId)
                .subscribe((result) => {
                    this.socialSecurityMaritalAllowance.set(result.socialSecurityMaritalAllowance);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SocialSecurityMaritalAllowance_SocialSecurityId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._socialSecurityMaritalAllowancesService
            .createOrEdit(this.socialSecurityMaritalAllowance())
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

    updateField(field: keyof CreateOrEditSocialSecurityMaritalAllowanceDto, value: any): void {
        const current = this.socialSecurityMaritalAllowance();
        (current as any)[field] = value;
        const next = new CreateOrEditSocialSecurityMaritalAllowanceDto();
        Object.assign(next, current);
        this.socialSecurityMaritalAllowance.set(next);
    }
}
