import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSurveyDto,
    EmployeeDto,
    EmployeeSurveysServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSurveysInput,
    SurveyDto,
    SurveysServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSurveyModal',
    templateUrl: './create-or-edit-employee-survey-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSurveyModalComponent extends AppComponentBase {
    private _employeeSurveysService = inject(EmployeeSurveysServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _surveysService = inject(SurveysServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly employeeSurvey = signal<CreateOrEditEmployeeSurveyDto>(new CreateOrEditEmployeeSurveyDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly surveys = signal<SurveyDto[]>([]);

    show(employeeSurveyId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._surveysService
            .getAll(
                new GetAllSurveysInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.surveys.set(result.items));

        if (!employeeSurveyId) {
            this.employeeSurvey.set(new CreateOrEditEmployeeSurveyDto());
            this.modal().show();
        } else {
            this._employeeSurveysService.getEmployeeSurveyForEdit(employeeSurveyId).subscribe((result) => {
                this.employeeSurvey.set(result.employeeSurvey);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSurvey_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    save(): void {
        this.saving.set(true);
        this._employeeSurveysService
            .createOrEdit(this.employeeSurvey())
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

    updateField(field: keyof CreateOrEditEmployeeSurveyDto, value: any): void {
        const current = this.employeeSurvey();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSurveyDto();
        Object.assign(next, current);
        this.employeeSurvey.set(next);
    }
}
