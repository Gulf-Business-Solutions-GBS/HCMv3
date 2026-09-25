import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeFundDetailDto,
    EmployeeFundDto,
    EmployeeFundDetailsServiceProxy,
    EmployeeFundsServiceProxy,
    FundPayType,
    GetAllEmployeeFundsInput,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeFundDetailModal',
    templateUrl: './create-or-edit-employee-fund-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CreateOrEditEmployeeFundDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeFundDetailsServiceProxy);
    private _employeeFundsService = inject(EmployeeFundsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeFundDetailDto>(new CreateOrEditEmployeeFundDetailDto());

    readonly employeeFunds = signal<EmployeeFundDto[]>([]);

    readonly fundPayTypes = [
        { value: FundPayType.Deduction, name: 'Deduction' },
        { value: FundPayType.Payment, name: 'Payment' },
        { value: FundPayType.Opening, name: 'Opening' },
        { value: FundPayType.ExternalDeduction, name: 'ExternalDeduction' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeeFundsService
            .getAll(new GetAllEmployeeFundsInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeFunds.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeFundDetailDto();
            record.amount = 0;
            record.fundPayType = FundPayType.Deduction;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeFundDetailForEdit(id).subscribe((result) => {
                this.record.set(result.employeeFundDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeFundDetail_EmployeeFundId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeFundDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeFundDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeFundDetailDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeFundDetailDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
