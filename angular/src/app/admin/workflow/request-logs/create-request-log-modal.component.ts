import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateRequestLogDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllWorkflowRequestsInput,
    RequestActionType,
    RequestLogsServiceProxy,
    WorkflowPlatform,
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
    selector: 'createRequestLogModal',
    templateUrl: './create-request-log-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateRequestLogModalComponent extends AppComponentBase {
    private _service = inject(RequestLogsServiceProxy);
    private _workflowRequestsService = inject(WorkflowRequestsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateRequestLogDto>(new CreateRequestLogDto());
    readonly workflowRequests = signal<WorkflowRequestDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    readonly actionTypes = [
        { value: RequestActionType.Created, key: 'RequestActionTypeCreated' },
        { value: RequestActionType.Approved, key: 'RequestActionTypeApproved' },
        { value: RequestActionType.Rejected, key: 'RequestActionTypeRejected' },
        { value: RequestActionType.Escalated, key: 'RequestActionTypeEscalated' },
        { value: RequestActionType.Read, key: 'RequestActionTypeRead' },
        { value: RequestActionType.SentBack, key: 'RequestActionTypeSentBack' },
        { value: RequestActionType.Closed, key: 'RequestActionTypeClosed' },
        { value: RequestActionType.Remarked, key: 'RequestActionTypeRemarked' },
        { value: RequestActionType.Assigned, key: 'RequestActionTypeAssigned' },
        { value: RequestActionType.Transferred, key: 'RequestActionTypeTransferred' },
        { value: RequestActionType.SentBackToCreator, key: 'RequestActionTypeSentBackToCreator' },
    ];

    readonly platforms = [
        { value: WorkflowPlatform.Core, key: 'WorkflowPlatformCore' },
        { value: WorkflowPlatform.ESS, key: 'WorkflowPlatformESS' },
        { value: WorkflowPlatform.Mobile, key: 'WorkflowPlatformMobile' },
    ];

    show(): void {
        this.active.set(true);

        this._workflowRequestsService
            .getAll(new GetAllWorkflowRequestsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, employeeId: undefined }))
            .subscribe((result) => this.workflowRequests.set(result.items));
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this.record.set(new CreateRequestLogDto({ actionType: RequestActionType.Created, platform: WorkflowPlatform.Core } as any));
        this.modal().show();
    }

    onShown(): void {
        document.getElementById('RequestLog_WorkflowRequestId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateRequestLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateRequestLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
