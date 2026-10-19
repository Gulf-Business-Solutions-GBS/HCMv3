import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AssetDto,
    AssetsServiceProxy,
    CreateOrEditEmployeeAssetInvoiceDto,
    EmployeeAssetInvoicesServiceProxy,
    GetAllAssetsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

@Component({
    selector: 'createOrEditEmployeeAssetInvoiceModal',
    templateUrl: './create-or-edit-employee-asset-invoice-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
    ],
})
export class CreateOrEditEmployeeAssetInvoiceModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAssetInvoicesServiceProxy);
    private _assetsService = inject(AssetsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAssetInvoiceDto>(new CreateOrEditEmployeeAssetInvoiceDto());

    readonly assets = signal<AssetDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._assetsService
            .getAll(new GetAllAssetsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.assets.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeAssetInvoiceDto();
            record.employeeId = employeeId;
            record.invoiceDate = DateTime.now().startOf('day');
            record.invoiceAmount = 0;
            record.employeePlanAmount = 0;
            record.deductionAmount = 0;
            record.deductFromEmployee = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAssetInvoiceForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAssetInvoice);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAssetInvoice_AssetId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAssetInvoiceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAssetInvoiceDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAssetInvoiceDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeAssetInvoiceDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
