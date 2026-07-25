import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    InterviewQuestionsServiceProxy,
    CreateOrEditJobDescriptionInterviewQuestionDto,
    JobDescriptionInterviewQuestionsServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditJobDescriptionInterviewQuestionModal',
    templateUrl: './create-or-edit-job-description-interview-question-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionInterviewQuestionModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionInterviewQuestionsServiceProxy);
    private _interviewQuestionsService = inject(InterviewQuestionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditJobDescriptionInterviewQuestionDto>(new CreateOrEditJobDescriptionInterviewQuestionDto());
    readonly interviewQuestions = signal<SimpleLookupDto[]>([]);

    show(jobDescriptionId: number, id?: number): void {
        this.active.set(true);

        this._interviewQuestionsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.interviewQuestions.set(result.items));

        if (!id) {
            const record = new CreateOrEditJobDescriptionInterviewQuestionDto();
            record.jobDescriptionId = jobDescriptionId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getJobDescriptionInterviewQuestionForEdit(id).subscribe((result) => {
                this.record.set(result.jobDescriptionInterviewQuestion);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescriptionInterviewQuestion_InterviewQuestionId')?.focus();
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

    updateField(field: keyof CreateOrEditJobDescriptionInterviewQuestionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionInterviewQuestionDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
