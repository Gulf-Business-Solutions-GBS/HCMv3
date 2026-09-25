import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditExecutionLogDto,
    ExecutionLogAction,
    ExecutionLogModule,
    ExecutionLogsServiceProxy,
    ExecutionLogStatus,
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
    selector: 'createOrEditExecutionLogModal',
    templateUrl: './create-or-edit-execution-log-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditExecutionLogModalComponent extends AppComponentBase {
    private _service = inject(ExecutionLogsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditExecutionLogDto>(new CreateOrEditExecutionLogDto());

    readonly actions = [
        { value: ExecutionLogAction.RUN, name: 'RUN' },
        { value: ExecutionLogAction.GENERATE, name: 'GENERATE' },
        { value: ExecutionLogAction.VIEW, name: 'VIEW' },
        { value: ExecutionLogAction.EXPORT, name: 'EXPORT' },
        { value: ExecutionLogAction.PRINT, name: 'PRINT' },
    ];

    readonly modules = [
        { value: ExecutionLogModule.TimeEngine, name: 'TimeEngine' },
        { value: ExecutionLogModule.PayrollEngine, name: 'PayrollEngine' },
        { value: ExecutionLogModule.BankFile, name: 'BankFile' },
        { value: ExecutionLogModule.GridReport, name: 'GridReport' },
        { value: ExecutionLogModule.PrintReport, name: 'PrintReport' },
        { value: ExecutionLogModule.CostEngine, name: 'CostEngine' },
        { value: ExecutionLogModule.ProjectEngine, name: 'ProjectEngine' },
    ];

    readonly statuses = [
        { value: ExecutionLogStatus.INPRECESS, name: 'INPRECESS' },
        { value: ExecutionLogStatus.SUCCESS, name: 'SUCCESS' },
        { value: ExecutionLogStatus.FAILED, name: 'FAILED' },
        { value: ExecutionLogStatus.PARTIAL, name: 'PARTIAL' },
        { value: ExecutionLogStatus.ABORTED, name: 'ABORTED' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditExecutionLogDto();
            record.action = ExecutionLogAction.RUN;
            record.module = ExecutionLogModule.TimeEngine;
            record.executionStatus = ExecutionLogStatus.INPRECESS;
            record.totalRecords = 0;
            record.successfull = 0;
            record.failed = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getExecutionLogForEdit(id).subscribe((result) => {
                this.record.set(result.executionLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ExecutionLog_UserName')?.focus();
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

    updateField(field: keyof CreateOrEditExecutionLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditExecutionLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    toDateTimeLocal(value?: DateTime): string {
        return value ? value.toFormat("yyyy-MM-dd'T'HH:mm") : '';
    }

    updateDateTimeField(field: keyof CreateOrEditExecutionLogDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditExecutionLogDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
