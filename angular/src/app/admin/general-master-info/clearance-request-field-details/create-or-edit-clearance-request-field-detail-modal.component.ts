import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ClearanceRequestFieldDetailsServiceProxy,
    CreateOrEditClearanceRequestFieldDetailDto,
    ClearanceRequestFieldDto,
    GetAllClearanceRequestFieldsInput,
    ClearanceRequestFieldsServiceProxy,
    GetAllWorkflowRolesInput,
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
    selector: 'createOrEditClearanceRequestFieldDetailModal',
    templateUrl: './create-or-edit-clearance-request-field-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditClearanceRequestFieldDetailModalComponent extends AppComponentBase {
    private _clearanceRequestFieldDetailService = inject(ClearanceRequestFieldDetailsServiceProxy);
    private _clearanceRequestFieldsService = inject(ClearanceRequestFieldsServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly clearanceRequestFieldDetail = signal<CreateOrEditClearanceRequestFieldDetailDto>(
        new CreateOrEditClearanceRequestFieldDetailDto()
    );
    readonly clearanceRequestFields = signal<ClearanceRequestFieldDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    /** Set when opened from a clearance request field's page: the detail belongs to that field and the choice is not offered. */
    readonly fixedClearanceRequestFieldId = signal<number | undefined>(undefined);

    show(clearanceRequestFieldDetailId?: number, clearanceRequestFieldId?: number): void {
        this.fixedClearanceRequestFieldId.set(clearanceRequestFieldId);
        this.active.set(true);

        this._clearanceRequestFieldsService
            .getAll(
                new GetAllClearanceRequestFieldsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.clearanceRequestFields.set(result.items);
            });

        // The role that has to answer this checklist line; the server requires it.
        this._workflowRolesService
            .getAll(
                new GetAllWorkflowRolesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.workflowRoles.set(result.items));

        if (!clearanceRequestFieldDetailId) {
            const record = new CreateOrEditClearanceRequestFieldDetailDto();
            record.clearanceRequestFieldId = clearanceRequestFieldId;
            this.clearanceRequestFieldDetail.set(record);
            this.modal().show();
        } else {
            this._clearanceRequestFieldDetailService
                .getClearanceRequestFieldDetailForEdit(clearanceRequestFieldDetailId)
                .subscribe((result) => {
                    this.clearanceRequestFieldDetail.set(result.clearanceRequestFieldDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document
            .getElementById(
                this.fixedClearanceRequestFieldId()
                    ? 'ClearanceRequestFieldDetail_WorkflowRoleId'
                    : 'ClearanceRequestFieldDetail_ClearanceRequestFieldId'
            )
            ?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._clearanceRequestFieldDetailService
            .createOrEdit(this.clearanceRequestFieldDetail())
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
    updateField(field: keyof CreateOrEditClearanceRequestFieldDetailDto, value: any): void {
        const current = this.clearanceRequestFieldDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditClearanceRequestFieldDetailDto();
        Object.assign(next, current);
        this.clearanceRequestFieldDetail.set(next);
    }
}
