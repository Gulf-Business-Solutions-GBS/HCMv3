import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeBasicPayDto,
    EmployeeBasicPaysServiceProxy,
    PayInterval,
    PayType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeBasicPayFormComponent } from './employee-basic-pay-form.component';

@Component({
    selector: 'createOrEditEmployeeBasicPayModal',
    templateUrl: './create-or-edit-employee-basic-pay-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe, EmployeeBasicPayFormComponent],
})
export class CreateOrEditEmployeeBasicPayModalComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPaysServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly employeeId = signal<number | undefined>(undefined);
    readonly record = signal<CreateOrEditEmployeeBasicPayDto>(new CreateOrEditEmployeeBasicPayDto());

    show(employeeId: number, id?: number): void {
        this.employeeId.set(employeeId);
        this.active.set(true);

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
}
