import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOnbAppTaskCommentsDto,
    GetAllOnbApplicantTasksInput,
    OnbAppTaskCommentsServiceProxy,
    OnbApplicantTasksDto,
    OnbApplicantTasksServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOnbAppTaskCommentModal',
    templateUrl: './create-or-edit-onb-app-task-comment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOnbAppTaskCommentModalComponent extends AppComponentBase {
    private _service = inject(OnbAppTaskCommentsServiceProxy);
    private _onbApplicantTasksService = inject(OnbApplicantTasksServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbAppTaskCommentsDto>(new CreateOrEditOnbAppTaskCommentsDto());

    readonly onbApplicantTasks = signal<OnbApplicantTasksDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._onbApplicantTasksService
            .getAll(new GetAllOnbApplicantTasksInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, applicantId: undefined }))
            .subscribe((result) => this.onbApplicantTasks.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditOnbAppTaskCommentsDto());
            this.modal().show();
        } else {
            this._service.getOnbAppTaskCommentsForEdit(id).subscribe((result) => {
                this.record.set(result.onbAppTaskComments);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OnbAppTaskComment_OnbApplicantTasksId')?.focus();
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

    updateField(field: keyof CreateOrEditOnbAppTaskCommentsDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbAppTaskCommentsDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
