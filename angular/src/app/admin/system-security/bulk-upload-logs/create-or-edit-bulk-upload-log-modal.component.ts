import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BulkUploadLogsServiceProxy,
    BulkUploadLogType,
    CreateOrEditBulkUploadLogDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
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
    selector: 'createOrEditBulkUploadLogModal',
    templateUrl: './create-or-edit-bulk-upload-log-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditBulkUploadLogModalComponent extends AppComponentBase {
    private _service = inject(BulkUploadLogsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditBulkUploadLogDto>(new CreateOrEditBulkUploadLogDto());
    readonly employees = signal<EmployeeDto[]>([]);

    readonly logTypes = [{ value: BulkUploadLogType.ProfilePhoto, name: 'ProfilePhoto' }];

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditBulkUploadLogDto();
            record.logType = BulkUploadLogType.ProfilePhoto;
            record.status = 0;
            record.stopFlag = false;
            record.noOfEmployee = 0;
            record.recordNo = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getBulkUploadLogForEdit(id).subscribe((result) => {
                this.record.set(result.bulkUploadLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('BulkUploadLog_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditBulkUploadLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditBulkUploadLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditBulkUploadLogDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    toDateTimeLocal(value?: DateTime): string {
        return value ? value.toFormat("yyyy-MM-dd'T'HH:mm") : '';
    }

    updateDateTimeField(field: keyof CreateOrEditBulkUploadLogDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditBulkUploadLogDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
