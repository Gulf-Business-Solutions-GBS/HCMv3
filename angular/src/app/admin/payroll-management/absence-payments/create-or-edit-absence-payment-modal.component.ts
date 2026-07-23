import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsencePaymentsServiceProxy,
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditAbsencePaymentDto,
    GetAllAbsenceTypesInput,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAbsencePaymentModal',
    templateUrl: './create-or-edit-absence-payment-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsencePaymentModalComponent extends AppComponentBase {
    private _absencePaymentsService = inject(AbsencePaymentsServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absencePayment = signal<CreateOrEditAbsencePaymentDto>(new CreateOrEditAbsencePaymentDto());
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);
    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(absencePaymentId?: number): void {
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!absencePaymentId) {
            this.absencePayment.set(new CreateOrEditAbsencePaymentDto());
            this.modal().show();
        } else {
            this._absencePaymentsService.getAbsencePaymentForEdit(absencePaymentId).subscribe((result) => {
                this.absencePayment.set(result.absencePayment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsencePayment_AbsenceTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absencePaymentsService
            .createOrEdit(this.absencePayment())
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

    updateField(field: keyof CreateOrEditAbsencePaymentDto, value: any): void {
        const current = this.absencePayment();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsencePaymentDto();
        Object.assign(next, current);
        this.absencePayment.set(next);
    }
}
