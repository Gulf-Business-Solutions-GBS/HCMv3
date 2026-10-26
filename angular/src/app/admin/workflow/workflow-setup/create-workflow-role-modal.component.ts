import { ChangeDetectionStrategy, Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import {
    CreateWorkflowSetupRoleInput,
    GetAllOrganizationUnitTypesInput,
    OrganizationUnitTypeDto,
    OrganizationUnitTypesServiceProxy,
    WorkflowRoleGroup,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    WorkflowSetupRoleDto,
    WorkflowSetupServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

/**
 * Creates a workflow role and puts people in it without leaving the setup grid.
 *
 * <p>The reason this exists is that a chain cannot be finished without a role to point the Person step at,
 * and sending somebody to another screen mid-edit loses the row they were working on. Whatever is created
 * here is an ordinary role -- it appears in Workflow Roles and can be edited there like any other.</p>
 *
 * <p>Members are given an assignment starting today and open-ended, because a role created to be used now
 * has to resolve now. A fixed term is a rarer thing and belongs on the detailed screen.</p>
 */
@Component({
    selector: 'createWorkflowRoleModal',
    templateUrl: './create-workflow-role-modal.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateWorkflowRoleModalComponent extends AppComponentBase {
    private _service = inject(WorkflowSetupServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createRoleModal');
    readonly modalSave = output<WorkflowSetupRoleDto>();

    readonly active = signal(false);
    readonly saving = signal(false);

    readonly code = signal('');
    readonly nameEn = signal('');
    readonly nameAr = signal('');

    private _orgLevelsService = inject(OrganizationUnitTypesServiceProxy);

    /** The same scoping the Workflow Roles screen offers, so a role created here is not a lesser one. */
    readonly roleGroup = signal<WorkflowRoleGroup>(WorkflowRoleGroup.None);
    readonly orgUnitId = signal<number | undefined>(undefined);
    readonly orgLevels = signal<OrganizationUnitTypeDto[]>([]);

    readonly roleGroups = [
        { value: WorkflowRoleGroup.None, key: 'WorkflowRoleGroupNone' },
        { value: WorkflowRoleGroup.Company, key: 'WorkflowRoleGroupCompany' },
        { value: WorkflowRoleGroup.Department, key: 'WorkflowRoleGroupDepartment' },
        { value: WorkflowRoleGroup.Branch, key: 'WorkflowRoleGroupBranch' },
        { value: WorkflowRoleGroup.CostCenter, key: 'WorkflowRoleGroupCostCenter' },
    ];
    readonly employees = signal<EmployeeDto[]>([]);
    readonly selected = signal<Set<number>>(new Set<number>());
    readonly filterText = signal('');

    readonly filteredEmployees = computed(() => {
        const filter = this.filterText().trim().toLowerCase();
        const all = this.employees();

        if (!filter) {
            return all;
        }

        return all.filter(
            (e) =>
                (e.fullNameEn ?? '').toLowerCase().includes(filter) ||
                (e.fullNameAr ?? '').includes(filter) ||
                (e.code ?? '').toLowerCase().includes(filter)
        );
    });

    readonly selectedCount = computed(() => this.selected().size);

    show(): void {
        this.active.set(true);
        this.code.set('');
        this.nameEn.set('');
        this.nameAr.set('');
        this.selected.set(new Set<number>());
        this.filterText.set('');
        this.roleGroup.set(WorkflowRoleGroup.None);
        this.orgUnitId.set(undefined);

        this._orgLevelsService
            .getAll(
                new GetAllOrganizationUnitTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.orgLevels.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this.modal().show();
    }

    isSelected(employeeId: number): boolean {
        return this.selected().has(employeeId);
    }

    toggle(employeeId: number): void {
        const next = new Set(this.selected());

        if (next.has(employeeId)) {
            next.delete(employeeId);
        } else {
            next.add(employeeId);
        }

        this.selected.set(next);
    }

    employeeLabel(employee: EmployeeDto): string {
        const name = this.isRtl
            ? employee.fullNameAr || employee.fullNameEn
            : employee.fullNameEn || employee.fullNameAr;

        return employee.code ? `${employee.code} — ${name}` : name;
    }

    save(): void {
        const input = new CreateWorkflowSetupRoleInput();
        input.code = this.code();
        input.nameEn = this.nameEn();

        // Arabic falls back to the English name rather than blocking the save. Both columns are required by
        // the entity, and making somebody stop to translate is what turns a two-minute setup into a task
        // for later.
        input.nameAr = this.nameAr()?.trim() || this.nameEn();
        input.workflowRoleGroup = this.roleGroup();
        input.workflowRoleOrgId = this.orgUnitId();
        input.employeeIds = Array.from(this.selected());

        this.saving.set(true);

        this._service
            .createRole(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((role) => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(role);
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
