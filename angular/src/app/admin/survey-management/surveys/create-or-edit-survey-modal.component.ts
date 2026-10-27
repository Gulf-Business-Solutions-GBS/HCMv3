import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSurveyDto, SurveysServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSurveyModal',
    templateUrl: './create-or-edit-survey-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSurveyModalComponent extends AppComponentBase {
    private _surveysService = inject(SurveysServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly survey = signal<CreateOrEditSurveyDto>(new CreateOrEditSurveyDto());

    show(surveyId?: number): void {
        this.active.set(true);

        if (!surveyId) {
            this.survey.set(new CreateOrEditSurveyDto());
            this.modal().show();
        } else {
            this._surveysService.getSurveyForEdit(surveyId).subscribe((result) => {
                this.survey.set(result.survey);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Survey_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._surveysService
            .createOrEdit(this.survey())
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

    updateField(field: keyof CreateOrEditSurveyDto, value: any): void {
        const current = this.survey();
        (current as any)[field] = value;
        const next = new CreateOrEditSurveyDto();
        Object.assign(next, current);
        this.survey.set(next);
    }

    updateDateField(field: keyof CreateOrEditSurveyDto, value: string): void {
        const current = this.survey();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditSurveyDto();
        Object.assign(next, current);
        this.survey.set(next);
    }
}
