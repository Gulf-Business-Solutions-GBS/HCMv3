import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BudgetingGroupsServiceProxy,
    CostCenterDto,
    CostCentersServiceProxy,
    CreateOrEditEmployeeOrgAssignmentDto,
    EmployeeOrgAssignmentsServiceProxy,
    GetAllCostCentersInput,
    GetAllSimpleLookupsInput,
    GetAllWorkLocationsInput,
    JobTitlesServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    PostingGroupsServiceProxy,
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

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeOrgAssignmentDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeOrgAssignmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeOrgAssignment);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        this._costCentersService
            .getAll(new GetAllCostCentersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.costCenters.set(result.items));

        this._workLocationsService
            .getAll(new GetAllWorkLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workLocations.set(result.items));

        this._postingGroupsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.postingGroups.set(result.items));

        this._budgetingGroupsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.budgetingGroups.set(result.items));

        this._organizationUnitService.getOrganizationUnits().subscribe((result) => this.organizationUnits.set(result.items ?? []));
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

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
