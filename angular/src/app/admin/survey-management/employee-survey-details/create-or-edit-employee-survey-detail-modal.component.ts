import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSurveyDetailDto,
    EmployeeSurveyDetailsServiceProxy,
    EmployeeSurveyDto,
    EmployeeSurveysServiceProxy,
    GetAllEmployeeSurveysInput,
    GetAllSurveyDetailsInput,
    SurveyDetailDto,
    SurveyDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSurveyDetailModal',
    templateUrl: './create-or-edit-employee-survey-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSurveyDetailModalComponent extends AppComponentBase {
    private _employeeSurveyDetailsService = inject(EmployeeSurveyDetailsServiceProxy);
    private _employeeSurveysService = inject(EmployeeSurveysServiceProxy);
    private _surveyDetailsService = inject(SurveyDetailsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly employeeSurveyDetail = signal<CreateOrEditEmployeeSurveyDetailDto>(new CreateOrEditEmployeeSurveyDetailDto());
    readonly employeeSurveys = signal<EmployeeSurveyDto[]>([]);
    readonly questions = signal<SurveyDetailDto[]>([]);

    show(employeeSurveyDetailId?: number): void {
        this.active.set(true);

        this._employeeSurveysService
            .getAll(
                new GetAllEmployeeSurveysInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                    surveyId: undefined,
                })
            )
            .subscribe((result) => this.employeeSurveys.set(result.items));

        this._surveyDetailsService
            .getAll(new GetAllSurveyDetailsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, surveyId: undefined }))
            .subscribe((result) => this.questions.set(result.items));

        if (!employeeSurveyDetailId) {
            this.employeeSurveyDetail.set(new CreateOrEditEmployeeSurveyDetailDto());
            this.modal().show();
        } else {
            this._employeeSurveyDetailsService.getEmployeeSurveyDetailForEdit(employeeSurveyDetailId).subscribe((result) => {
                this.employeeSurveyDetail.set(result.employeeSurveyDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSurveyDetail_EmployeeSurveyId')?.focus();
    }

    employeeSurveyLabel(employeeSurvey: EmployeeSurveyDto): string {
        return `${employeeSurvey.employeeCode} - ${employeeSurvey.surveyNameEn}`;
    }

    save(): void {
        this.saving.set(true);
        this._employeeSurveyDetailsService
            .createOrEdit(this.employeeSurveyDetail())
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

    updateField(field: keyof CreateOrEditEmployeeSurveyDetailDto, value: any): void {
        const current = this.employeeSurveyDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSurveyDetailDto();
        Object.assign(next, current);
        this.employeeSurveyDetail.set(next);
    }
}
