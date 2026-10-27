import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPunchNotificationLogDto,
    EmailNotificationLogDto,
    EmailNotificationLogsServiceProxy,
    EmployeeDutyPeriodDto,
    EmployeeDutyPeriodsServiceProxy,
    GetAllEmailNotificationLogsInput,
    GetAllEmployeeDutyPeriodsInput,
    PunchNotificationLogsServiceProxy,
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
    selector: 'createOrEditPunchNotificationLogModal',
    templateUrl: './create-or-edit-punch-notification-log-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPunchNotificationLogModalComponent extends AppComponentBase {
    private _service = inject(PunchNotificationLogsServiceProxy);
    private _employeeDutyPeriodsService = inject(EmployeeDutyPeriodsServiceProxy);
    private _emailNotificationLogsService = inject(EmailNotificationLogsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPunchNotificationLogDto>(new CreateOrEditPunchNotificationLogDto());
    readonly employeeDutyPeriods = signal<EmployeeDutyPeriodDto[]>([]);
    readonly emailNotificationLogs = signal<EmailNotificationLogDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeeDutyPeriodsService
            .getAll(
                new GetAllEmployeeDutyPeriodsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.employeeDutyPeriods.set(result.items));

        this._emailNotificationLogsService
            .getAll(
                new GetAllEmailNotificationLogsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.emailNotificationLogs.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditPunchNotificationLogDto());
            this.modal().show();
        } else {
            this._service.getPunchNotificationLogForEdit(id).subscribe((result) => {
                this.record.set(result.punchNotificationLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PunchNotificationLog_EmployeeDutyPeriodId')?.focus();
    }

    employeeDutyPeriodLabel(item: EmployeeDutyPeriodDto): string {
        return `${item.employeeFullNameEn} - ${item.dutyPeriodName}`;
    }

    emailNotificationLogLabel(item: EmailNotificationLogDto): string {
        return `${item.email} (${item.subject})`;
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

    updateField(field: keyof CreateOrEditPunchNotificationLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPunchNotificationLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditPunchNotificationLogDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditPunchNotificationLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
