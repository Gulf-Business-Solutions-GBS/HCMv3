import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateWFReadLogDto,
    GetAllWorkflowRequestsInput,
    WFReadLogsServiceProxy,
    WorkflowRequestDto,
    WorkflowRequestsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createWFReadLogModal',
    templateUrl: './create-wf-read-log-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateWFReadLogModalComponent extends AppComponentBase {
    private _service = inject(WFReadLogsServiceProxy);
    private _workflowRequestsService = inject(WorkflowRequestsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateWFReadLogDto>(new CreateWFReadLogDto());
    readonly workflowRequests = signal<WorkflowRequestDto[]>([]);

    show(): void {
        this.active.set(true);

        this._workflowRequestsService
            .getAll(new GetAllWorkflowRequestsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, employeeId: undefined }))
            .subscribe((result) => this.workflowRequests.set(result.items));

        this.record.set(new CreateWFReadLogDto());
        this.modal().show();
    }

    onShown(): void {
        document.getElementById('WFReadLog_WorkflowRequestId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .create(this.record())
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

    updateField(field: keyof CreateWFReadLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateWFReadLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
