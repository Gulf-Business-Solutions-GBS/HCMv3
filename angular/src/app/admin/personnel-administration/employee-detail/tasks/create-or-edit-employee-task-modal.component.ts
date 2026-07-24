import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTaskDto,
    EmployeeTasksServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    TaskTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeTaskModal',
    templateUrl: './create-or-edit-employee-task-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTaskModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTasksServiceProxy);
    private _taskTypesService = inject(TaskTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTaskDto>(new CreateOrEditEmployeeTaskDto());

    readonly taskTypes = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._taskTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.taskTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTaskDto();
            record.employeeId = employeeId;
            record.taskStatus = 0;
            record.monitoringType = 0;
            record.monitoringCount = 0;
            record.monitoringUnit = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTaskForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTask);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeTask_TaskTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTaskDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTaskDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeTaskDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTaskDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
