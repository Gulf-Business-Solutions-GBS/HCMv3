import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPayrollControlRecordDto,
    GetAllPayrollAreasInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollControlRecordsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPayrollControlRecordModal',
    templateUrl: './create-or-edit-payroll-control-record-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayrollControlRecordModalComponent extends AppComponentBase {
    private _payrollControlRecordsService = inject(PayrollControlRecordsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly payrollControlRecord = signal<CreateOrEditPayrollControlRecordDto>(new CreateOrEditPayrollControlRecordDto());
    readonly payrollAreas = signal<PayrollAreaDto[]>([]);

    show(payrollControlRecordId?: number): void {
        this.active.set(true);

        this._payrollAreasService
            .getAll(new GetAllPayrollAreasInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.payrollAreas.set(result.items));

        if (!payrollControlRecordId) {
            this.payrollControlRecord.set(new CreateOrEditPayrollControlRecordDto());
            this.modal().show();
        } else {
            this._payrollControlRecordsService.getPayrollControlRecordForEdit(payrollControlRecordId).subscribe((result) => {
                this.payrollControlRecord.set(result.payrollControlRecord);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayrollControlRecord_PayrollAreaId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._payrollControlRecordsService
            .createOrEdit(this.payrollControlRecord())
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

    updateField(field: keyof CreateOrEditPayrollControlRecordDto, value: any): void {
        const current = this.payrollControlRecord();
        (current as any)[field] = value;
        const next = new CreateOrEditPayrollControlRecordDto();
        Object.assign(next, current);
        this.payrollControlRecord.set(next);
    }
}
