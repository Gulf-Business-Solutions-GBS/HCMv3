import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSalaryRegisterDto,
    GetAllSimpleLookupsInput,
    PayrollReasonTypesServiceProxy,
    PayrollResultType,
    PayrollRunType,
    SalaryRegistersServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditSalaryRegisterModal',
    templateUrl: './create-or-edit-salary-register-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSalaryRegisterModalComponent extends AppComponentBase {
    private _salaryRegistersService = inject(SalaryRegistersServiceProxy);
    private _payrollReasonTypesService = inject(PayrollReasonTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly salaryRegister = signal<CreateOrEditSalaryRegisterDto>(new CreateOrEditSalaryRegisterDto());

    readonly payrollReasonTypes = signal<SimpleLookupDto[]>([]);

    readonly payrollRunTypes = [
        { value: PayrollRunType.Regular, name: 'Regular' },
        { value: PayrollRunType.OffCycle, name: 'OffCycle' },
    ];

    readonly payrollResultTypes = [
        { value: PayrollResultType.Current, name: 'Current' },
        { value: PayrollResultType.Last, name: 'Last' },
        { value: PayrollResultType.Archive, name: 'Archive' },
    ];

    show(salaryRegisterId?: number): void {
        this.active.set(true);

        this._payrollReasonTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.payrollReasonTypes.set(result.items));

        if (!salaryRegisterId) {
            this.salaryRegister.set(new CreateOrEditSalaryRegisterDto());
            this.modal().show();
        } else {
            this._salaryRegistersService.getSalaryRegisterForEdit(salaryRegisterId).subscribe((result) => {
                this.salaryRegister.set(result.salaryRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SalaryRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._salaryRegistersService
            .createOrEdit(this.salaryRegister())
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

    updateField(field: keyof CreateOrEditSalaryRegisterDto, value: any): void {
        const current = this.salaryRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditSalaryRegisterDto();
        Object.assign(next, current);
        this.salaryRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditSalaryRegisterDto, value: string): void {
        this.updateField(field, value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }
}
