import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeBasicPayDto,
    EmployeeActionDto,
    EmployeeActionsServiceProxy,
    EmployeeBasicPaysServiceProxy,
    GetAllEmployeeActionsInput,
    PayInterval,
    PayType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeBasicPayModal',
    templateUrl: './create-or-edit-employee-basic-pay-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe, LuxonFormatPipe],
})
export class CreateOrEditEmployeeBasicPayModalComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPaysServiceProxy);
    private _employeeActionsService = inject(EmployeeActionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeBasicPayDto>(new CreateOrEditEmployeeBasicPayDto());

    readonly employeeActions = signal<EmployeeActionDto[]>([]);

    readonly payTypes = [
        { value: PayType.None, name: 'None' },
        { value: PayType.Regular, name: 'Regular' },
    ];

    readonly payIntervals = [
        { value: PayInterval.Monthly, name: 'Monthly' },
        { value: PayInterval.Daily, name: 'Daily' },
        { value: PayInterval.Hourly, name: 'Hourly' },
        { value: PayInterval.EndOfService, name: 'EndOfService' },
        { value: PayInterval.Yearly, name: 'Yearly' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._employeeActionsService
            .getAll(new GetAllEmployeeActionsInput({ employeeId, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeActions.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeBasicPayDto();
            record.employeeId = employeeId;
            record.payType = PayType.Regular;
            record.payInterval = PayInterval.Monthly;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeBasicPayForEdit(id).subscribe((result) => {
                this.record.set(result.employeeBasicPay);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeBasicPay_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeBasicPayDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeBasicPayDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeBasicPayDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
