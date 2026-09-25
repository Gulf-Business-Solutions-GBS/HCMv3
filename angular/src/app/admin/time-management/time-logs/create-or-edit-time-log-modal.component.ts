import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTimeLogDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    TimeLogsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditTimeLogModal',
    templateUrl: './create-or-edit-time-log-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTimeLogModalComponent extends AppComponentBase {
    private _timeLogsService = inject(TimeLogsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timeLog = signal<CreateOrEditTimeLogDto>(new CreateOrEditTimeLogDto());
    readonly employees = signal<EmployeeDto[]>([]);

    show(timeLogId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!timeLogId) {
            this.timeLog.set(new CreateOrEditTimeLogDto());
            this.modal().show();
        } else {
            this._timeLogsService.getTimeLogForEdit(timeLogId).subscribe((result) => {
                this.timeLog.set(result.timeLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimeLog_RunTimeStamp')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timeLogsService
            .createOrEdit(this.timeLog())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditTimeLogDto, value: any): void {
        const current = this.timeLog();
        (current as any)[field] = value;
        const next = new CreateOrEditTimeLogDto();
        Object.assign(next, current);
        this.timeLog.set(next);
    }

    updateDateField(field: keyof CreateOrEditTimeLogDto, value: string): void {
        const current = this.timeLog();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeLogDto();
        Object.assign(next, current);
        this.timeLog.set(next);
    }

    toDateTimeLocal(value?: DateTime): string {
        return value ? value.toFormat("yyyy-MM-dd'T'HH:mm") : '';
    }

    updateDateTimeField(field: keyof CreateOrEditTimeLogDto, value: string): void {
        const current = this.timeLog();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeLogDto();
        Object.assign(next, current);
        this.timeLog.set(next);
    }
}
