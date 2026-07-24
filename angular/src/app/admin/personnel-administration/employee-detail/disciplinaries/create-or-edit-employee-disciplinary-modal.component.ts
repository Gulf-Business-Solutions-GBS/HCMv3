import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDisciplinaryDto,
    DisciplinaryDeductionTypeDto,
    DisciplinaryDeductionTypesServiceProxy,
    DisciplinaryTypeDto,
    DisciplinaryTypesServiceProxy,
    EmployeeDisciplinariesServiceProxy,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllDisciplinaryDeductionTypesInput,
    GetAllDisciplinaryTypesInput,
    GetAllEmployeesInput,
    GetAllWorkLocationsInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    Severity,
    WorkLocationDto,
    WorkLocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeDisciplinaryModal',
    templateUrl: './create-or-edit-employee-disciplinary-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeDisciplinaryModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDisciplinariesServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);
    private _disciplinaryTypesService = inject(DisciplinaryTypesServiceProxy);
    private _disciplinaryDeductionTypesService = inject(DisciplinaryDeductionTypesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeDisciplinaryDto>(new CreateOrEditEmployeeDisciplinaryDto());

    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly workLocations = signal<WorkLocationDto[]>([]);
    readonly disciplinaryTypes = signal<DisciplinaryTypeDto[]>([]);
    readonly disciplinaryDeductionTypes = signal<DisciplinaryDeductionTypeDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    readonly severities = [
        { value: Severity.Low, name: 'Low' },
        { value: Severity.High, name: 'High' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeDisciplinaryDto();
            record.employeeId = employeeId;
            record.severity = Severity.Low;
            record.wlSendToEmp = false;
            record.wlRecvFromEmp = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeDisciplinaryForEdit(id).subscribe((result) => {
                this.record.set(result.employeeDisciplinary);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._organizationUnitService.getOrganizationUnits().subscribe((result) => this.organizationUnits.set(result.items ?? []));

        this._workLocationsService
            .getAll(new GetAllWorkLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workLocations.set(result.items));

        this._disciplinaryTypesService
            .getAll(new GetAllDisciplinaryTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.disciplinaryTypes.set(result.items));

        this._disciplinaryDeductionTypesService
            .getAll(new GetAllDisciplinaryDeductionTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.disciplinaryDeductionTypes.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeDisciplinary_DisciplinaryTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeDisciplinaryDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDisciplinaryDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeDisciplinaryDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditEmployeeDisciplinaryDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditEmployeeDisciplinaryDto, value: string): void {
        if (!value) {
            this.updateField(field, undefined as any);
            return;
        }
        const [hour, minute, second] = value.split(':').map((part) => Number(part));
        const current = (this.record() as any)[field] as DateTime | undefined;
        const base = current ?? DateTime.now();
        this.updateField(field, base.set({ hour, minute, second: second || 0, millisecond: 0 }));
    }
}
