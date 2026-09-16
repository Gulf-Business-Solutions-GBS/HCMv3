import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDedRuleDetailsServiceProxy,
    AbsenceDeductionRuleDto,
    AbsenceDeductionRulesServiceProxy,
    CreateOrEditAbsenceDedRuleDetailDto,
    DeductionUnit,
    GetAllAbsenceDeductionRulesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditAbsenceDedRuleDetailModal',
    templateUrl: './create-or-edit-absence-ded-rule-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceDedRuleDetailModalComponent extends AppComponentBase {
    private _absenceDedRuleDetailsService = inject(AbsenceDedRuleDetailsServiceProxy);
    private _absenceDeductionRulesService = inject(AbsenceDeductionRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceDedRuleDetail = signal<CreateOrEditAbsenceDedRuleDetailDto>(new CreateOrEditAbsenceDedRuleDetailDto());
    readonly absenceDeductionRules = signal<AbsenceDeductionRuleDto[]>([]);

    readonly deductionUnits = [
        { value: DeductionUnit.Fixed, name: 'Fixed' },
        { value: DeductionUnit.Percentage, name: 'Percentage' },
    ];

    /** Set when opened from an absence deduction rule's detail page: the rule is fixed. */
    readonly presetAbsenceDeductionRuleId = signal<number | undefined>(undefined);

    show(absenceDedRuleDetailId?: number, absenceDeductionRuleId?: number): void {
        this.presetAbsenceDeductionRuleId.set(absenceDeductionRuleId);
        this.active.set(true);

        this._absenceDeductionRulesService
            .getAll(new GetAllAbsenceDeductionRulesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceDeductionRules.set(result.items));

        if (!absenceDedRuleDetailId) {
            const dto = new CreateOrEditAbsenceDedRuleDetailDto();
            if (absenceDeductionRuleId) {
                dto.absenceDeductionRuleId = absenceDeductionRuleId;
            }
            this.absenceDedRuleDetail.set(dto);
            this.modal().show();
        } else {
            this._absenceDedRuleDetailsService.getAbsenceDedRuleDetailForEdit(absenceDedRuleDetailId).subscribe((result) => {
                this.absenceDedRuleDetail.set(result.absenceDedRuleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const firstFieldId = this.presetAbsenceDeductionRuleId()
            ? 'AbsenceDedRuleDetail_StartDate'
            : 'AbsenceDedRuleDetail_AbsenceDeductionRuleId';
        document.getElementById(firstFieldId)?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceDedRuleDetailsService
            .createOrEdit(this.absenceDedRuleDetail())
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

    updateField(field: keyof CreateOrEditAbsenceDedRuleDetailDto, value: any): void {
        const current = this.absenceDedRuleDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceDedRuleDetailDto();
        Object.assign(next, current);
        this.absenceDedRuleDetail.set(next);
    }

    updateDateField(field: keyof CreateOrEditAbsenceDedRuleDetailDto, value: string): void {
        const current = this.absenceDedRuleDetail();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditAbsenceDedRuleDetailDto();
        Object.assign(next, current);
        this.absenceDedRuleDetail.set(next);
    }
}
