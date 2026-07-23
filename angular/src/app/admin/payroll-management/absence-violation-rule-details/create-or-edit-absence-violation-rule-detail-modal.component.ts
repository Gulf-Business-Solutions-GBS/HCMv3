import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceViolationAction,
    AbsenceViolationRuleDetailsServiceProxy,
    AbsenceViolationRulesServiceProxy,
    CreateOrEditAbsenceViolationRuleDetailDto,
    GetAllSimpleLookupsInput,
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
    selector: 'createOrEditAbsenceViolationRuleDetailModal',
    templateUrl: './create-or-edit-absence-violation-rule-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceViolationRuleDetailModalComponent extends AppComponentBase {
    private _absenceViolationRuleDetailsService = inject(AbsenceViolationRuleDetailsServiceProxy);
    private _absenceViolationRulesService = inject(AbsenceViolationRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceViolationRuleDetail = signal<CreateOrEditAbsenceViolationRuleDetailDto>(
        new CreateOrEditAbsenceViolationRuleDetailDto()
    );
    readonly absenceViolationRules = signal<SimpleLookupDto[]>([]);

    readonly actions = [
        { value: AbsenceViolationAction.Notification, name: 'Notification' },
        { value: AbsenceViolationAction.Violation, name: 'Violation' },
        { value: AbsenceViolationAction.Leave, name: 'Leave' },
    ];

    show(absenceViolationRuleDetailId?: number): void {
        this.active.set(true);

        this._absenceViolationRulesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceViolationRules.set(result.items));

        if (!absenceViolationRuleDetailId) {
            this.absenceViolationRuleDetail.set(new CreateOrEditAbsenceViolationRuleDetailDto());
            this.modal().show();
        } else {
            this._absenceViolationRuleDetailsService.getAbsenceViolationRuleDetailForEdit(absenceViolationRuleDetailId).subscribe((result) => {
                this.absenceViolationRuleDetail.set(result.absenceViolationRuleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceViolationRuleDetail_AbsenceViolationRuleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceViolationRuleDetailsService
            .createOrEdit(this.absenceViolationRuleDetail())
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

    updateField(field: keyof CreateOrEditAbsenceViolationRuleDetailDto, value: any): void {
        const current = this.absenceViolationRuleDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceViolationRuleDetailDto();
        Object.assign(next, current);
        this.absenceViolationRuleDetail.set(next);
    }
}
