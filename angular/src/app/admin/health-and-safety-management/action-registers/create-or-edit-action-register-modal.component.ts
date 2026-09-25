import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ActionRegistersServiceProxy,
    ActionStatusesServiceProxy,
    CreateOrEditActionRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllIncidentRegistersInput,
    GetAllRiskRegistersInput,
    GetAllSimpleLookupsInput,
    IncidentRegisterDto,
    IncidentRegistersServiceProxy,
    RiskRegisterDto,
    RiskRegistersServiceProxy,
    SimpleLookupDto,
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

@Component({
    selector: 'createOrEditActionRegisterModal',
    templateUrl: './create-or-edit-action-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditActionRegisterModalComponent extends AppComponentBase {
    private _service = inject(ActionRegistersServiceProxy);
    private _incidentRegistersService = inject(IncidentRegistersServiceProxy);
    private _riskRegistersService = inject(RiskRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _actionStatusesService = inject(ActionStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditActionRegisterDto>(new CreateOrEditActionRegisterDto());

    readonly incidentRegisters = signal<IncidentRegisterDto[]>([]);
    readonly riskRegisters = signal<RiskRegisterDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);
    readonly actionStatuses = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditActionRegisterDto());
            this.modal().show();
        } else {
            this._service.getActionRegisterForEdit(id).subscribe((result) => {
                this.record.set(result.actionRegister);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._incidentRegistersService
            .getAll(new GetAllIncidentRegistersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, incidentTypeId: undefined, statusId: undefined }))
            .subscribe((result) => this.incidentRegisters.set(result.items));

        this._riskRegistersService
            .getAll(new GetAllRiskRegistersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, riskTypeId: undefined, statusId: undefined }))
            .subscribe((result) => this.riskRegisters.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._actionStatusesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.actionStatuses.set(result.items));
    }

    onShown(): void {
        document.getElementById('ActionRegister_ActionDate')?.focus();
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

    updateField(field: keyof CreateOrEditActionRegisterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditActionRegisterDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'actionDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
