import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSurveyDetailDto,
    GetAllSurveysInput,
    QuestionType,
    SurveyDetailsServiceProxy,
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
    selector: 'createOrEditSurveyDetailModal',
    templateUrl: './create-or-edit-survey-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSurveyDetailModalComponent extends AppComponentBase {
    private _surveyDetailsService = inject(SurveyDetailsServiceProxy);
    private _surveysService = inject(SurveysServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly surveyDetail = signal<CreateOrEditSurveyDetailDto>(new CreateOrEditSurveyDetailDto());
    readonly surveys = signal<SurveyDto[]>([]);

    readonly questionTypes = [
        { value: QuestionType.TextBox, name: 'TextBox' },
        { value: QuestionType.RadioList, name: 'RadioList' },
        { value: QuestionType.CheckboxList, name: 'CheckboxList' },
    ];

    show(surveyDetailId?: number): void {
        this.active.set(true);

        this._surveysService
            .getAll(
                new GetAllSurveysInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.surveys.set(result.items));

        if (!surveyDetailId) {
            this.surveyDetail.set(new CreateOrEditSurveyDetailDto());
            this.modal().show();
        } else {
            this._surveyDetailsService.getSurveyDetailForEdit(surveyDetailId).subscribe((result) => {
                this.surveyDetail.set(result.surveyDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SurveyDetail_SurveyId')?.focus();
    }

    isChoiceQuestion(): boolean {
        const type = this.surveyDetail().questionType;
        return type === QuestionType.RadioList || type === QuestionType.CheckboxList;
    }

    save(): void {
        this.saving.set(true);
        this._surveyDetailsService
            .createOrEdit(this.surveyDetail())
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

    updateField(field: keyof CreateOrEditSurveyDetailDto, value: any): void {
        const current = this.surveyDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSurveyDetailDto();
        Object.assign(next, current);
        this.surveyDetail.set(next);
    }
}
