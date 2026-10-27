import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSocialSecurityEducationAllowanceDto,
    GetAllSocialSecuritiesInput,
    GetAllSimpleLookupsInput,
    SocialSecurityDto,
    SocialSecuritiesServiceProxy,
    SocialSecurityEducationAllowancesServiceProxy,
    EducationBranchesServiceProxy,
    EducationsServiceProxy,
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
    selector: 'createOrEditSocialSecurityEducationAllowanceModal',
    templateUrl: './create-or-edit-social-security-education-allowance-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSocialSecurityEducationAllowanceModalComponent extends AppComponentBase {
    private _socialSecurityEducationAllowancesService = inject(SocialSecurityEducationAllowancesServiceProxy);
    private _socialSecuritiesService = inject(SocialSecuritiesServiceProxy);
    private _educationBranchesService = inject(EducationBranchesServiceProxy);
    private _educationsService = inject(EducationsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly socialSecurityEducationAllowance = signal<CreateOrEditSocialSecurityEducationAllowanceDto>(
        new CreateOrEditSocialSecurityEducationAllowanceDto()
    );
    readonly socialSecurities = signal<SocialSecurityDto[]>([]);
    readonly educationBranches = signal<SimpleLookupDto[]>([]);
    readonly educations = signal<SimpleLookupDto[]>([]);

    show(socialSecurityEducationAllowanceId?: number): void {
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

        this._educationsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.educations.set(result.items));

        if (!socialSecurityEducationAllowanceId) {
            this.socialSecurityEducationAllowance.set(new CreateOrEditSocialSecurityEducationAllowanceDto());
            this.modal().show();
        } else {
            this._socialSecurityEducationAllowancesService
                .getSocialSecurityEducationAllowanceForEdit(socialSecurityEducationAllowanceId)
                .subscribe((result) => {
                    this.socialSecurityEducationAllowance.set(result.socialSecurityEducationAllowance);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SocialSecurityEducationAllowance_SocialSecurityId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._socialSecurityEducationAllowancesService
            .createOrEdit(this.socialSecurityEducationAllowance())
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

    updateField(field: keyof CreateOrEditSocialSecurityEducationAllowanceDto, value: any): void {
        const current = this.socialSecurityEducationAllowance();
        (current as any)[field] = value;
        const next = new CreateOrEditSocialSecurityEducationAllowanceDto();
        Object.assign(next, current);
        this.socialSecurityEducationAllowance.set(next);
    }
}
