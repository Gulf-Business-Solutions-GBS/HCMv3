import { Component, input, model } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditAbsenceDeductionRuleDto } from '@shared/service-proxies/service-proxies';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * The absence deduction rule's own fields, shared by the Create/Edit modal and the rule detail page.
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through
 * the view provider below, so the host's Save button sees their validity exactly as it did when the
 * fields lived directly in the modal.
 */
@Component({
    selector: 'absenceDeductionRuleForm',
    templateUrl: './absence-deduction-rule-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class AbsenceDeductionRuleFormComponent extends AppComponentBase {
    readonly absenceDeductionRule = model.required<CreateOrEditAbsenceDeductionRuleDto>();
    readonly readOnly = input<boolean>(false);

    updateField(field: keyof CreateOrEditAbsenceDeductionRuleDto, value: any): void {
        const next = new CreateOrEditAbsenceDeductionRuleDto();
        Object.assign(next, this.absenceDeductionRule());
        (next as any)[field] = value;
        this.absenceDeductionRule.set(next);
    }
}
