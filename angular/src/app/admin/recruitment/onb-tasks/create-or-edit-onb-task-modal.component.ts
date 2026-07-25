import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOnbTaskDto,
    GetAllOnbTemplatesInput,
    OnbTasksServiceProxy,
    OnbTemplateDto,
    OnbTemplatesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOnbTaskModal',
    templateUrl: './create-or-edit-onb-task-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOnbTaskModalComponent extends AppComponentBase {
    private _service = inject(OnbTasksServiceProxy);
    private _onbTemplatesService = inject(OnbTemplatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbTaskDto>(new CreateOrEditOnbTaskDto());

    readonly onbTemplates = signal<OnbTemplateDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._onbTemplatesService
            .getAll(new GetAllOnbTemplatesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.onbTemplates.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditOnbTaskDto());
            this.modal().show();
        } else {
            this._service.getOnbTaskForEdit(id).subscribe((result) => {
                this.record.set(result.onbTask);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OnbTask_TaskNameEn')?.focus();
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

    updateField(field: keyof CreateOrEditOnbTaskDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbTaskDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
