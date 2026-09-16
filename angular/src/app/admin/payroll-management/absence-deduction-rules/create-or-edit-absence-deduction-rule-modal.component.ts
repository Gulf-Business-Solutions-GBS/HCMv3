import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDeductionRulesServiceProxy,
    CreateOrEditAbsenceDeductionRuleDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AbsenceDeductionRuleFormComponent } from './absence-deduction-rule-form.component';

@Component({
    selector: 'createOrEditAbsenceDeductionRuleModal',
    templateUrl: './create-or-edit-absence-deduction-rule-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ButtonBusyDirective,
        LocalizePipe,
        AbsenceDeductionRuleFormComponent,
    ],
})
export class CreateOrEditAbsenceDeductionRuleModalComponent extends AppComponentBase {
    private _absenceDeductionRulesService = inject(AbsenceDeductionRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceDeductionRule = signal<CreateOrEditAbsenceDeductionRuleDto>(new CreateOrEditAbsenceDeductionRuleDto());

    show(absenceDeductionRuleId?: number): void {
        this.active.set(true);

        if (!absenceDeductionRuleId) {
            this.absenceDeductionRule.set(new CreateOrEditAbsenceDeductionRuleDto());
            this.modal().show();
        } else {
            this._absenceDeductionRulesService.getAbsenceDeductionRuleForEdit(absenceDeductionRuleId).subscribe((result) => {
                this.absenceDeductionRule.set(result.absenceDeductionRule);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceDeductionRule_RuleName')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceDeductionRulesService
            .createOrEdit(this.absenceDeductionRule())
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
}
