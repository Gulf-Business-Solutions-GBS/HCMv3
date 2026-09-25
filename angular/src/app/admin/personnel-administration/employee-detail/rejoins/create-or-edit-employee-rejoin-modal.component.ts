import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeRejoinDto, EmployeeRejoinsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeRejoinModal',
    templateUrl: './create-or-edit-employee-rejoin-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeRejoinModalComponent extends AppComponentBase {
    private _service = inject(EmployeeRejoinsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeRejoinDto>(new CreateOrEditEmployeeRejoinDto());

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeeRejoinDto();
            record.employeeId = employeeId;
            record.carryForwardLeaveBalance = false;
            record.carryForwardTicketBalance = false;
            record.carryForwardIndemnityBalance = false;
            record.carryForwardLoanBalance = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeRejoinForEdit(id).subscribe((result) => {
                this.record.set(result.employeeRejoin);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeRejoin_RejoinDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeRejoinDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeRejoinDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateRejoinDate(value: string): void {
        this.updateField('rejoinDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
