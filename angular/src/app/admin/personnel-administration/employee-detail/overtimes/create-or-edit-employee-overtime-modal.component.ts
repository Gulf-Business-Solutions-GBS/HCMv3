import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeOvertimeDto,
    EmployeeOvertimesServiceProxy,
    GetAllOvertimeTypesInput,
    OvertimeTypeDto,
    OvertimeTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeOvertimeModal',
    templateUrl: './create-or-edit-employee-overtime-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeOvertimeModalComponent extends AppComponentBase {
    private _service = inject(EmployeeOvertimesServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeOvertimeDto>(new CreateOrEditEmployeeOvertimeDto());

    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._overtimeTypesService
            .getAll(new GetAllOvertimeTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.overtimeTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeOvertimeDto();
            record.employeeId = employeeId;
            record.overtimeHours = 0;
            record.actualOvertimeHours = 0;
            record.approvedOvertimeHours = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeOvertimeForEdit(id).subscribe((result) => {
                this.record.set(result.employeeOvertime);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeOvertime_OvertimeTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeOvertimeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeOvertimeDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeOvertimeDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeOvertimeDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
