import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDeductionRuleDto,
    AbsenceDeductionRulesServiceProxy,
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeSubGroupAbsenceDto,
    EmployeeSubGroupAbsencesServiceProxy,
    GetAllAbsenceDeductionRulesInput,
    GetAllAbsenceTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditEmployeeSubGroupAbsenceModal',
    templateUrl: './create-or-edit-employee-sub-group-absence-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupAbsenceModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupAbsencesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);
    private _absenceDeductionRulesService = inject(AbsenceDeductionRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupAbsenceDto>(new CreateOrEditEmployeeSubGroupAbsenceDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeSubGroupAbsence',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeSubGroupId: this.record().employeeSubGroupId, AbsenceTypeId: this.record().absenceTypeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);
    readonly absenceDeductionRules = signal<AbsenceDeductionRuleDto[]>([]);

    show(employeeSubGroupId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        this._absenceDeductionRulesService
            .getAll(new GetAllAbsenceDeductionRulesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceDeductionRules.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupAbsenceDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupAbsenceForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupAbsence);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupAbsence_AbsenceTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupAbsenceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupAbsenceDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateEndDate(value: string): void {
        this.updateField('endDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
