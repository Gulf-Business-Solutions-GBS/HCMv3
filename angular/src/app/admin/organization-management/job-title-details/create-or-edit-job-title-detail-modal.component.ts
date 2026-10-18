import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    JobTitleDetailsServiceProxy,
    CreateOrEditJobTitleDetailDto,
    GetAllSimpleLookupsInput,
    GetAllWorkflowRolesInput,
    JobTitlesServiceProxy,
    SimpleLookupDto,
    WorkflowRoleDto,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditJobTitleDetailModal',
    templateUrl: './create-or-edit-job-title-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditJobTitleDetailModalComponent extends AppComponentBase {
    private _jobTitleDetailService = inject(JobTitleDetailsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly jobTitleDetail = signal<CreateOrEditJobTitleDetailDto>(new CreateOrEditJobTitleDetailDto());
    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    show(jobTitleDetailId?: number): void {
        this.active.set(true);

        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        this._workflowRolesService
            .getAll(new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workflowRoles.set(result.items));

        if (!jobTitleDetailId) {
            this.jobTitleDetail.set(new CreateOrEditJobTitleDetailDto());
            this.modal().show();
        } else {
            this._jobTitleDetailService.getJobTitleDetailForEdit(jobTitleDetailId).subscribe((result) => {
                this.jobTitleDetail.set(result.jobTitleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobTitleDetail_JobTitleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._jobTitleDetailService
            .createOrEdit(this.jobTitleDetail())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditJobTitleDetailDto, value: any): void {
        const current = this.jobTitleDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditJobTitleDetailDto();
        Object.assign(next, current);
        this.jobTitleDetail.set(next);
    }
}
