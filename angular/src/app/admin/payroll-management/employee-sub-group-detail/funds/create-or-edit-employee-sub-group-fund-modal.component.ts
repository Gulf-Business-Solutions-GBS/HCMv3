import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSubGroupFundDto,
    EmployeeSubGroupFundsServiceProxy,
    FundDedType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSubGroupFundModal',
    templateUrl: './create-or-edit-employee-sub-group-fund-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupFundModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupFundsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupFundDto>(new CreateOrEditEmployeeSubGroupFundDto());

    readonly fundDedTypes = [
        { value: FundDedType.Percentage, name: 'Percentage' },
        { value: FundDedType.PercentageWithinLimit, name: 'PercentageWithinLimit' },
        { value: FundDedType.FixAmount, name: 'FixAmount' },
    ];

    show(employeeSubGroupId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupFundDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupFundForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupFund);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupFund_FromPay')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupFundDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupFundDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
