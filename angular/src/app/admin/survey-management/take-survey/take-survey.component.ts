import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    ActiveSurveyLookupDto,
    EmployeeSurveyAnswerInput,
    EmployeeSurveyQuestionDto,
    EmployeeSurveyQuestionnaireDto,
    EmployeeSurveyResponseServiceProxy,
    GetEmployeeSurveyQuestionnaireInput,
    QuestionType,
    SubmitEmployeeSurveyInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of the legacy "take a survey" screen
 * (HCM10.Web/UserControls/SurveyManagement/uscEmployeeSurvey.ascx + .ascx.cs).
 *
 * Legacy rendered a DevExpress ASPxGridView whose "Answer" column used a per-row ITemplate that built an
 * ASPxTextBox / ASPxRadioButtonList / ASPxCheckBoxList (EditorTemplate, legacy lines 542-777). That editor
 * factory is WebForms presentation and does not port; the QuestionType now travels on each question row and
 * the editor is chosen here.
 *
 * Two legacy string conventions are preserved EXACTLY because stored answers must stay compatible:
 *   - SurveyDetail.Options is split on ';'   (legacy lines 646, 670, 690)
 *   - a multi-select ANSWER is joined on ',' (legacy lines 105-111, 666)
 */
@Component({
    templateUrl: './take-survey.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class TakeSurveyComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeSurveyResponseServiceProxy);

    readonly surveys = signal<ActiveSurveyLookupDto[]>([]);
    readonly selectedSurveyId = signal<number | undefined>(undefined);
    readonly questionnaire = signal<EmployeeSurveyQuestionnaireDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    /** Working answers, keyed by SurveyDetailId. Checkbox answers are held as string[] until submit. */
    private readonly _answers = new Map<number, string | string[]>();

    readonly QuestionType = QuestionType;

    ngOnInit(): void {
        this._service.getActiveSurveys().subscribe((result) => this.surveys.set(result));
    }

    /** Legacy GetData (lines 197-272). */
    loadQuestionnaire(): void {
        const surveyId = this.selectedSurveyId();
        if (!surveyId) {
            this.questionnaire.set(undefined);
            return;
        }

        this.loading.set(true);
        this._service
            .getEmployeeSurveyQuestionnaire(
                new GetEmployeeSurveyQuestionnaireInput({ surveyId, employeeId: undefined })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this._answers.clear();

                for (const question of result.questions || []) {
                    if (question.questionType === QuestionType.CheckboxList) {
                        this._answers.set(
                            question.surveyDetailId,
                            question.answer ? question.answer.split(',').map((v) => v.trim()) : []
                        );
                    } else {
                        this._answers.set(question.surveyDetailId, question.answer || '');
                    }
                }

                this.questionnaire.set(result);
            });
    }

    /** Legacy split the option string on ';' -- see class remarks. */
    options(question: EmployeeSurveyQuestionDto): string[] {
        if (!question.options) {
            return [];
        }
        return question.options.split(';').map((v) => v.trim());
    }

    textAnswer(question: EmployeeSurveyQuestionDto): string {
        const value = this._answers.get(question.surveyDetailId);
        return typeof value === 'string' ? value : '';
    }

    setTextAnswer(question: EmployeeSurveyQuestionDto, value: string): void {
        this._answers.set(question.surveyDetailId, value ?? '');
    }

    isOptionSelected(question: EmployeeSurveyQuestionDto, option: string): boolean {
        const value = this._answers.get(question.surveyDetailId);
        return Array.isArray(value) ? value.indexOf(option) >= 0 : value === option;
    }

    toggleOption(question: EmployeeSurveyQuestionDto, option: string, checked: boolean): void {
        const current = this._answers.get(question.surveyDetailId);
        const list = Array.isArray(current) ? current.slice() : [];
        const index = list.indexOf(option);

        if (checked && index < 0) {
            list.push(option);
        } else if (!checked && index >= 0) {
            list.splice(index, 1);
        }

        this._answers.set(question.surveyDetailId, list);
    }

    /** Legacy SubmitSurveyReponse (lines 273-343). */
    submit(): void {
        const questionnaire = this.questionnaire();
        if (!questionnaire) {
            return;
        }

        if (!questionnaire.questions || questionnaire.questions.length === 0) {
            // Legacy lines 123-127.
            this.message.info(this.l('NoSurveyDataFoundToSave'));
            return;
        }

        const answers = questionnaire.questions.map((question) => {
            const value = this._answers.get(question.surveyDetailId);
            return new EmployeeSurveyAnswerInput({
                surveyDetailId: question.surveyDetailId,
                // A multi-select answer is stored comma-joined, exactly as legacy built it.
                answer: Array.isArray(value) ? value.join(',') : (value ?? ''),
            });
        });

        this.saving.set(true);
        this._service
            .submitEmployeeSurvey(
                new SubmitEmployeeSurveyInput({
                    surveyId: questionnaire.surveyId,
                    employeeId: questionnaire.employeeId,
                    answers,
                })
            )
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('EmployeeSurveySubmitted'));
                this.loadQuestionnaire();
            });
    }
}
