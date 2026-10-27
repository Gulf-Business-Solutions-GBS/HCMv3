import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BudgetingGroupsServiceProxy,
    CostCenterDto,
    CostCentersServiceProxy,
    CreateOrEditEmployeeOrgAssignmentDto,
    EmployeeGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeOrgAssignmentsServiceProxy,
    EmployeeSubGroupDto,
    EmployeeSubGroupsServiceProxy,
    GetAllCostCentersInput,
    GetAllEmployeeGroupsInput,
    GetAllEmployeeSubGroupsInput,
    GetAllPayrollAreasInput,
    GetAllSimpleLookupsInput,
    GetAllWorkflowGroupsInput,
    GetAllWorkLocationsInput,
    GetPositionsForOrgAssignmentInput,
    JobTitlesServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PositionForOrgAssignmentDto,
    PostingGroupsServiceProxy,
    SimpleLookupDto,
    WorkflowGroupDto,
    WorkflowGroupsServiceProxy,
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
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeOrgAssignmentModal',
    templateUrl: './create-or-edit-employee-org-assignment-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeOrgAssignmentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeOrgAssignmentsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _costCentersService = inject(CostCentersServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);
    private _postingGroupsService = inject(PostingGroupsServiceProxy);
    private _budgetingGroupsService = inject(BudgetingGroupsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _workflowGroupsService = inject(WorkflowGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeOrgAssignmentDto>(new CreateOrEditEmployeeOrgAssignmentDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeOrgAssignment',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly costCenters = signal<CostCenterDto[]>([]);
    readonly workLocations = signal<WorkLocationDto[]>([]);
    readonly postingGroups = signal<SimpleLookupDto[]>([]);
    readonly budgetingGroups = signal<SimpleLookupDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly employeeGroups = signal<EmployeeGroupDto[]>([]);
    readonly employeeSubGroups = signal<EmployeeSubGroupDto[]>([]);
    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly workflowGroups = signal<WorkflowGroupDto[]>([]);

    /** Legacy EmpPosition: active, vacant positions, plus the one this assignment already holds. */
    readonly positions = signal<PositionForOrgAssignmentDto[]>([]);

    /** Only the chosen group's sub groups; the server rejects a sub group from another group. */
    readonly filteredEmployeeSubGroups = computed(() => {
        const groupId = this.record().employeeGroupId;
        const all = this.employeeSubGroups();
        return groupId ? all.filter((item) => item.employeeGroupId === groupId) : all;
    });

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeOrgAssignmentDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.loadPositions(undefined);
            this.modal().show();
        } else {
            this._service.getEmployeeOrgAssignmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeOrgAssignment);
                this.loadPositions(result.employeeOrgAssignment.positionId);
                this.modal().show();
            });
        }
    }

    private loadPositions(currentPositionId: number | undefined): void {
        this._service
            .getPositionsForOrgAssignment(new GetPositionsForOrgAssignmentInput({ currentPositionId }))
            .subscribe((result) => this.positions.set(result.items));
    }

    /**
     * Legacy OnChanged("Position"): choosing a position fills the job title, groups, payroll area, work location,
     * cost centre and org unit from it. Only values the position has are copied; the server applies the same rule.
     */
    updatePosition(positionId: number | undefined): void {
        const next = new CreateOrEditEmployeeOrgAssignmentDto();
        Object.assign(next, this.record());
        next.positionId = positionId;

        const position = this.positions().find((p) => p.id === positionId);
        if (position) {
            if (position.employeeGroupId && position.employeeGroupId !== next.employeeGroupId && !position.employeeSubGroupId) {
                next.employeeSubGroupId = undefined;
            }
            next.jobTitleId = position.jobTitleId ?? next.jobTitleId;
            next.employeeGroupId = position.employeeGroupId ?? next.employeeGroupId;
            next.employeeSubGroupId = position.employeeSubGroupId ?? next.employeeSubGroupId;
            next.payrollAreaId = position.payrollAreaId ?? next.payrollAreaId;
            next.workLocationId = position.workLocationId ?? next.workLocationId;
            next.costCenterId = position.costCenterId ?? next.costCenterId;
            next.organizationUnitId = position.organizationUnitId ?? next.organizationUnitId;
        }

        this.record.set(next);
    }

    private loadLookups(): void {
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

        this._postingGroupsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.postingGroups.set(result.items));

        this._budgetingGroupsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.budgetingGroups.set(result.items));

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result) => this.organizationUnits.set(result.items ?? []));

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
                    employeeGroupId: undefined,
                    filter: undefined,
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

        this._workflowGroupsService
            .getAll(
                new GetAllWorkflowGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.workflowGroups.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeOrgAssignment_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeOrgAssignmentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeOrgAssignmentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    /** Changing the group clears a sub group that belongs to a different group. */
    updateEmployeeGroup(value: number | undefined): void {
        const subGroup = this.employeeSubGroups().find((item) => item.id === this.record().employeeSubGroupId);
        this.updateField('employeeGroupId', value);
        if (value && subGroup && subGroup.employeeGroupId !== value) {
            this.updateField('employeeSubGroupId', undefined);
        }
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
