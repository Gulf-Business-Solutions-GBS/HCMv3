import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSocialSecurityDetailDto,
    GetAllSocialSecuritiesInput,
    SocialSecurityDto,
    SocialSecuritiesServiceProxy,
    SocialSecurityDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSocialSecurityDetailModal',
    templateUrl: './create-or-edit-social-security-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSocialSecurityDetailModalComponent extends AppComponentBase {
    private _socialSecurityDetailsService = inject(SocialSecurityDetailsServiceProxy);
    private _socialSecuritiesService = inject(SocialSecuritiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly socialSecurityDetail = signal<CreateOrEditSocialSecurityDetailDto>(
        new CreateOrEditSocialSecurityDetailDto()
    );
    readonly socialSecurities = signal<SocialSecurityDto[]>([]);

    show(socialSecurityDetailId?: number): void {
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

        if (!socialSecurityDetailId) {
            this.socialSecurityDetail.set(new CreateOrEditSocialSecurityDetailDto());
            this.modal().show();
        } else {
            this._socialSecurityDetailsService
                .getSocialSecurityDetailForEdit(socialSecurityDetailId)
                .subscribe((result) => {
                    this.socialSecurityDetail.set(result.socialSecurityDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SocialSecurityDetail_SocialSecurityId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._socialSecurityDetailsService
            .createOrEdit(this.socialSecurityDetail())
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

    updateField(field: keyof CreateOrEditSocialSecurityDetailDto, value: any): void {
        const current = this.socialSecurityDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSocialSecurityDetailDto();
        Object.assign(next, current);
        this.socialSecurityDetail.set(next);
    }
}
