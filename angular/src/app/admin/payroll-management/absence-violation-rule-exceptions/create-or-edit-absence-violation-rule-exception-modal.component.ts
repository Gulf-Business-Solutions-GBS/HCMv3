import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceViolationRuleExceptionsServiceProxy,
    AbsenceViolationRulesServiceProxy,
    CreateOrEditAbsenceViolationRuleExceptionDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
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
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditAbsenceViolationRuleExceptionModal',
    templateUrl: './create-or-edit-absence-violation-rule-exception-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceViolationRuleExceptionModalComponent extends AppComponentBase {
    private _absenceViolationRuleExceptionsService = inject(AbsenceViolationRuleExceptionsServiceProxy);
    private _absenceViolationRulesService = inject(AbsenceViolationRulesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceViolationRuleException = signal<CreateOrEditAbsenceViolationRuleExceptionDto>(
        new CreateOrEditAbsenceViolationRuleExceptionDto()
    );
    readonly absenceViolationRules = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(absenceViolationRuleExceptionId?: number): void {
        this.active.set(true);

        this._absenceViolationRulesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceViolationRules.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!absenceViolationRuleExceptionId) {
            this.absenceViolationRuleException.set(new CreateOrEditAbsenceViolationRuleExceptionDto());
            this.modal().show();
        } else {
            this._absenceViolationRuleExceptionsService
                .getAbsenceViolationRuleExceptionForEdit(absenceViolationRuleExceptionId)
                .subscribe((result) => {
                    this.absenceViolationRuleException.set(result.absenceViolationRuleException);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceViolationRuleException_AbsenceViolationRuleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceViolationRuleExceptionsService
            .createOrEdit(this.absenceViolationRuleException())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditAbsenceViolationRuleExceptionDto, value: any): void {
        const current = this.absenceViolationRuleException();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceViolationRuleExceptionDto();
        Object.assign(next, current);
        this.absenceViolationRuleException.set(next);
    }

    updateDateField(field: keyof CreateOrEditAbsenceViolationRuleExceptionDto, value: string): void {
        const current = this.absenceViolationRuleException();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditAbsenceViolationRuleExceptionDto();
        Object.assign(next, current);
        this.absenceViolationRuleException.set(next);
    }
}
