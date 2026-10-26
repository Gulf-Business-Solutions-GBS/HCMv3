import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditManpowerPlanningDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllManpowerPlanningPeriodsInput,
    ListResultDtoOfOrganizationUnitDto,
    ManpowerPlanningPeriodDto,
    ManpowerPlanningPeriodsServiceProxy,
    ManpowerPlanningsServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManpowerPlanningModal',
    templateUrl: './create-or-edit-manpower-planning-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManpowerPlanningModalComponent extends AppComponentBase {
    private _service = inject(ManpowerPlanningsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _periodsService = inject(ManpowerPlanningPeriodsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditManpowerPlanningDto>(new CreateOrEditManpowerPlanningDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly periods = signal<ManpowerPlanningPeriodDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._periodsService
            .getAll(
                new GetAllManpowerPlanningPeriodsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.periods.set(result.items));

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result: ListResultDtoOfOrganizationUnitDto) => this.organizationUnits.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditManpowerPlanningDto());
            this.modal().show();
        } else {
            this._service.getManpowerPlanningForEdit(id).subscribe((result) => {
                this.record.set(result.manpowerPlanning);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ManpowerPlanning_EmployeeId')?.focus();
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

    updateField(field: keyof CreateOrEditManpowerPlanningDto, value: any): void {
        const current = this.record();
        const next = new CreateOrEditManpowerPlanningDto();
        Object.assign(next, current);
        (next as any)[field] = value;
        this.record.set(next);
    }
}
