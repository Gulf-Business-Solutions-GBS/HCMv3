import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTimeTransferDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    TimeTransferTypesServiceProxy,
    TimeTransfersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditTimeTransferModal',
    templateUrl: './create-or-edit-time-transfer-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTimeTransferModalComponent extends AppComponentBase {
    private _timeTransfersService = inject(TimeTransfersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _timeTransferTypesService = inject(TimeTransferTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timeTransfer = signal<CreateOrEditTimeTransferDto>(new CreateOrEditTimeTransferDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly transferTypes = signal<SimpleLookupDto[]>([]);

    show(timeTransferId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._timeTransferTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.transferTypes.set(result.items));

        if (!timeTransferId) {
            this.timeTransfer.set(new CreateOrEditTimeTransferDto());
            this.modal().show();
        } else {
            this._timeTransfersService.getTimeTransferForEdit(timeTransferId).subscribe((result) => {
                this.timeTransfer.set(result.timeTransfer);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimeTransfer_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timeTransfersService
            .createOrEdit(this.timeTransfer())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditTimeTransferDto, value: any): void {
        const current = this.timeTransfer();
        (current as any)[field] = value;
        const next = new CreateOrEditTimeTransferDto();
        Object.assign(next, current);
        this.timeTransfer.set(next);
    }

    updateDateField(field: keyof CreateOrEditTimeTransferDto, value: string): void {
        const current = this.timeTransfer();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeTransferDto();
        Object.assign(next, current);
        this.timeTransfer.set(next);
    }
}
