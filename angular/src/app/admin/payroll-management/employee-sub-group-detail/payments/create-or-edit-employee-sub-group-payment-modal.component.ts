import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSubGroupPaymentDto,
    CustomStartMonth,
    EmployeeSubGroupPaymentsServiceProxy,
    GetAllWageTypesInput,
    PayInterval,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSubGroupPaymentModal',
    templateUrl: './create-or-edit-employee-sub-group-payment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupPaymentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupPaymentsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupPaymentDto>(new CreateOrEditEmployeeSubGroupPaymentDto());
    readonly wageTypes = signal<WageTypeDto[]>([]);

    readonly payIntervals = [
        { value: PayInterval.Monthly, name: 'Monthly' },
        { value: PayInterval.Daily, name: 'Daily' },
        { value: PayInterval.Hourly, name: 'Hourly' },
        { value: PayInterval.EndOfService, name: 'EndOfService' },
        { value: PayInterval.Yearly, name: 'Yearly' },
    ];

    readonly customStartMonths = [
        { value: CustomStartMonth.None, name: 'None' },
        { value: CustomStartMonth.January, name: 'January' },
        { value: CustomStartMonth.February, name: 'February' },
        { value: CustomStartMonth.March, name: 'March' },
        { value: CustomStartMonth.April, name: 'April' },
        { value: CustomStartMonth.May, name: 'May' },
        { value: CustomStartMonth.June, name: 'June' },
        { value: CustomStartMonth.July, name: 'July' },
        { value: CustomStartMonth.August, name: 'August' },
        { value: CustomStartMonth.September, name: 'September' },
        { value: CustomStartMonth.October, name: 'October' },
        { value: CustomStartMonth.November, name: 'November' },
        { value: CustomStartMonth.December, name: 'December' },
    ];

    show(employeeSubGroupId: number, id?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupPaymentDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupPaymentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupPayment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupPayment_WageTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupPaymentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupPaymentDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
