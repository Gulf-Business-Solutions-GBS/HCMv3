import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostCenterDto,
    CostCentersServiceProxy,
    CreateOrEditPositionDto,
    EmployeeGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeSubGroupDto,
    EmployeeSubGroupsServiceProxy,
    GetAllCostCentersInput,
    GetAllEmployeeGroupsInput,
    GetAllEmployeeSubGroupsInput,
    GetAllPayrollAreasInput,
    GetAllPositionsInput,
    GetAllSimpleLookupsInput,
    GetAllWorkLocationsInput,
    JobTitlesServiceProxy,
    ListResultDtoOfOrganizationUnitDto,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PositionDto,
    PositionsServiceProxy,
    SimpleLookupDto,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditPositionModal',
    templateUrl: './create-or-edit-position-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPositionModalComponent extends AppComponentBase {
    private _service = inject(PositionsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);
    private _costCentersService = inject(CostCentersServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPositionDto>(new CreateOrEditPositionDto());

    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly employeeGroups = signal<EmployeeGroupDto[]>([]);
    readonly employeeSubGroups = signal<EmployeeSubGroupDto[]>([]);
    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly workLocations = signal<WorkLocationDto[]>([]);
    readonly costCenters = signal<CostCenterDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);

    /**
     * Candidate parents for the position hierarchy the Position Org Chart draws. The position being
     * edited is filtered out -- nothing may report to itself, and the server rejects it anyway.
     */
    readonly parentPositions = signal<PositionDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._jobTitlesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobTitles.set(result.items));

        this._employeeGroupsService
            .getAll(
                new GetAllEmployeeGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.employeeGroups.set(result.items));

        this._employeeSubGroupsService
            .getAll(
                new GetAllEmployeeSubGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    employeeGroupId: undefined,
                })
            )
            .subscribe((result) => this.employeeSubGroups.set(result.items));

        this._payrollAreasService
            .getAll(
                new GetAllPayrollAreasInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.payrollAreas.set(result.items));

        this._workLocationsService
            .getAll(
                new GetAllWorkLocationsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.workLocations.set(result.items));

        this._costCentersService
            .getAll(
                new GetAllCostCentersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.costCenters.set(result.items));

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result: ListResultDtoOfOrganizationUnitDto) => this.organizationUnits.set(result.items));

        this._service
            .getAll(
                new GetAllPositionsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    vacant: undefined,
                })
            )
            .subscribe((result) => this.parentPositions.set(result.items.filter((p) => p.id !== id)));

        if (!id) {
            this.record.set(new CreateOrEditPositionDto());
            this.modal().show();
        } else {
            this._service.getPositionForEdit(id).subscribe((result) => {
                this.record.set(result.position);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Position_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditPositionDto, value: any): void {
        const current = this.record();
        const next = new CreateOrEditPositionDto();
        Object.assign(next, current);
        (next as any)[field] = value;
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditPositionDto, value: string): void {
        const current = this.record();
        const next = new CreateOrEditPositionDto();
        Object.assign(next, current);
        (next as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }
}
