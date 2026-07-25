import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRequestDetailParallelDto,
    GetAllRequestDetailsInput,
    GetAllRequestMastersInput,
    GetAllSimpleLookupsInput,
    GetAllWorkflowRolesInput,
    JobTitlesServiceProxy,
    RequestDetailDto,
    RequestDetailParallelsServiceProxy,
    RequestDetailsServiceProxy,
    RequestMasterDto,
    RequestMastersServiceProxy,
    SimpleLookupDto,
    WorkflowRoleDto,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRequestDetailParallelModal',
    templateUrl: './create-or-edit-request-detail-parallel-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditRequestDetailParallelModalComponent extends AppComponentBase {
    private _service = inject(RequestDetailParallelsServiceProxy);
    private _requestDetailsService = inject(RequestDetailsServiceProxy);
    private _requestMastersService = inject(RequestMastersServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRequestDetailParallelDto>(new CreateOrEditRequestDetailParallelDto());
    readonly requestDetails = signal<RequestDetailDto[]>([]);
    readonly requestMasters = signal<RequestMasterDto[]>([]);
    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._requestMastersService
            .getAll(new GetAllRequestMastersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.requestMasters.set(result.items));
        this._requestDetailsService
            .getAll(new GetAllRequestDetailsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, requestMasterId: undefined }))
            .subscribe((result) => this.requestDetails.set(result.items));
        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));
        this._workflowRolesService
            .getAll(new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workflowRoles.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditRequestDetailParallelDto());
            this.modal().show();
        } else {
            this._service.getRequestDetailParallelForEdit(id).subscribe((result) => {
                this.record.set(result.requestDetailParallel);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RequestDetailParallel_RequestDetailId')?.focus();
    }

    requestDetailLabel(detail: RequestDetailDto): string {
        const masterName = this.requestMasters().find((m) => m.id === detail.requestMasterId)?.nameEn;
        return `${masterName ?? ''} - ${this.l('Step')} ${detail.step}`;
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

    updateField(field: keyof CreateOrEditRequestDetailParallelDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditRequestDetailParallelDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
