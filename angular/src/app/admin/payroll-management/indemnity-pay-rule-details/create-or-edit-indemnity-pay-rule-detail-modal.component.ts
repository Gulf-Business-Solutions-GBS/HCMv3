import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditIndemnityPayRuleDetailDto,
    GetAllIndemnityPayRulesInput,
    IndemnityPayRuleDetailsServiceProxy,
    IndemnityPayRuleDto,
    IndemnityPayRulesServiceProxy,
    Unit,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditIndemnityPayRuleDetailModal',
    templateUrl: './create-or-edit-indemnity-pay-rule-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditIndemnityPayRuleDetailModalComponent extends AppComponentBase {
    private _indemnityPayRuleDetailsService = inject(IndemnityPayRuleDetailsServiceProxy);
    private _indemnityPayRulesService = inject(IndemnityPayRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly indemnityPayRuleDetail = signal<CreateOrEditIndemnityPayRuleDetailDto>(new CreateOrEditIndemnityPayRuleDetailDto());
    readonly indemnityPayRules = signal<IndemnityPayRuleDto[]>([]);

    readonly units = [
        { value: Unit.Day, name: 'Day' },
        { value: Unit.Month, name: 'Month' },
    ];

    show(indemnityPayRuleDetailId?: number): void {
        this.active.set(true);

        this._indemnityPayRulesService
            .getAll(new GetAllIndemnityPayRulesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.indemnityPayRules.set(result.items));

        if (!indemnityPayRuleDetailId) {
            this.indemnityPayRuleDetail.set(new CreateOrEditIndemnityPayRuleDetailDto());
            this.modal().show();
        } else {
            this._indemnityPayRuleDetailsService.getIndemnityPayRuleDetailForEdit(indemnityPayRuleDetailId).subscribe((result) => {
                this.indemnityPayRuleDetail.set(result.indemnityPayRuleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('IndemnityPayRuleDetail_IndemnityPayRuleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._indemnityPayRuleDetailsService
            .createOrEdit(this.indemnityPayRuleDetail())
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

    updateField(field: keyof CreateOrEditIndemnityPayRuleDetailDto, value: any): void {
        const current = this.indemnityPayRuleDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditIndemnityPayRuleDetailDto();
        Object.assign(next, current);
        this.indemnityPayRuleDetail.set(next);
    }
}
