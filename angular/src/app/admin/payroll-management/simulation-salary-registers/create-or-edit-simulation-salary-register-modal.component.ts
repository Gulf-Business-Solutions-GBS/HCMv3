import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSimulationSalaryRegisterDto,
    GetAllSimpleLookupsInput,
    PayrollReasonTypesServiceProxy,
    PayrollResultType,
    PayrollRunType,
    SimulationSalaryRegistersServiceProxy,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSimulationSalaryRegisterModal',
    templateUrl: './create-or-edit-simulation-salary-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSimulationSalaryRegisterModalComponent extends AppComponentBase {
    private _simulationSalaryRegistersService = inject(SimulationSalaryRegistersServiceProxy);
    private _payrollReasonTypesService = inject(PayrollReasonTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly simulationSalaryRegister = signal<CreateOrEditSimulationSalaryRegisterDto>(new CreateOrEditSimulationSalaryRegisterDto());

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

    show(simulationSalaryRegisterId?: number): void {
        this.active.set(true);

        this._payrollReasonTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.payrollReasonTypes.set(result.items));

        if (!simulationSalaryRegisterId) {
            this.simulationSalaryRegister.set(new CreateOrEditSimulationSalaryRegisterDto());
            this.modal().show();
        } else {
            this._simulationSalaryRegistersService.getSimulationSalaryRegisterForEdit(simulationSalaryRegisterId).subscribe((result) => {
                this.simulationSalaryRegister.set(result.simulationSalaryRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SimulationSalaryRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._simulationSalaryRegistersService
            .createOrEdit(this.simulationSalaryRegister())
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

    updateField(field: keyof CreateOrEditSimulationSalaryRegisterDto, value: any): void {
        const current = this.simulationSalaryRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditSimulationSalaryRegisterDto();
        Object.assign(next, current);
        this.simulationSalaryRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditSimulationSalaryRegisterDto, value: string): void {
        this.updateField(field, value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }
}
