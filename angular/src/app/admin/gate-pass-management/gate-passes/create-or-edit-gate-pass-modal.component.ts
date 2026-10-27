import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditGatePassDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GatePassesServiceProxy,
    GatePassMovementsServiceProxy,
    GatePassStatusesServiceProxy,
    GatePassTypesServiceProxy,
    GetAllEmployeesInput,
    GetAllLocationsInput,
    GetAllSimpleLookupsInput,
    LocationDto,
    LocationsServiceProxy,
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
    selector: 'createOrEditGatePassModal',
    templateUrl: './create-or-edit-gate-pass-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditGatePassModalComponent extends AppComponentBase {
    private _service = inject(GatePassesServiceProxy);
    private _gatePassTypesService = inject(GatePassTypesServiceProxy);
    private _gatePassMovementsService = inject(GatePassMovementsServiceProxy);
    private _gatePassStatusesService = inject(GatePassStatusesServiceProxy);
    private _locationsService = inject(LocationsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditGatePassDto>(new CreateOrEditGatePassDto());

    readonly gatePassTypes = signal<SimpleLookupDto[]>([]);
    readonly gatePassMovements = signal<SimpleLookupDto[]>([]);
    readonly gatePassStatuses = signal<SimpleLookupDto[]>([]);
    readonly locations = signal<LocationDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditGatePassDto());
            this.modal().show();
        } else {
            this._service.getGatePassForEdit(id).subscribe((result) => {
                this.record.set(result.gatePass);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._gatePassTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.gatePassTypes.set(result.items));

        this._gatePassMovementsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.gatePassMovements.set(result.items));

        this._gatePassStatusesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.gatePassStatuses.set(result.items));

        this._locationsService
            .getAll(
                new GetAllLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.locations.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('GatePass_GatePassNumber')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditGatePassDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditGatePassDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'issueDate' | 'validUpto', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateTimeField(field: 'startTime' | 'endTime', dateField: 'issueDate' | 'validUpto', value: string): void {
        const current = this.record();
        if (!value) {
            (current as any)[field] = undefined;
        } else {
            const base = (current as any)[dateField] ?? DateTime.now();
            (current as any)[field] = DateTime.fromFormat(
                `${base.toFormat('yyyy-MM-dd')} ${value}`,
                'yyyy-MM-dd HH:mm'
            );
        }
        const next = new CreateOrEditGatePassDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
