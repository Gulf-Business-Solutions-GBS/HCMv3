import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import {
    ApprovalRequire,
    SaveWorkflowSetupRowInput,
    WorkflowApproverPersonType,
    WorkflowPersonScope,
    WorkflowSetupRoleDto,
    WorkflowSetupRowDto,
    WorkflowSetupStepDto,
    WorkflowSetupServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { CreateWorkflowRoleModalComponent } from './create-workflow-role-modal.component';

/** How many approver positions the grid shows. The server locks anything longer. */
const MAX_STEPS = 3;

/**
 * Configures every transaction from one row instead of four screens.
 *
 * <p>The grid writes the same tables the detailed screens do and stores nothing of its own. What it cannot
 * express -- parallel approvers, chains shared between transactions, per-step signatory or read-log
 * settings -- comes back locked from the server rather than flattened, because saving a simple row over a
 * complicated chain would delete working configuration with nothing on screen to say so.</p>
 *
 * <p>An approver cell speaks the engine's own vocabulary: <b>Person</b> means whoever holds a workflow
 * role, and the manager levels walk the requester's reporting line. There is no "pick an employee" option
 * because the engine has no such field -- naming one person means a role with one member, which is also
 * what survives that person leaving.</p>
 */
@Component({
    templateUrl: './workflow-setup.component.html',
    animations: [appModuleAnimation()],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormsModule,
        RouterLink,
        SubHeaderComponent,
        LocalizePipe,
        ButtonBusyDirective,
        CreateWorkflowRoleModalComponent,
    ],
})
export class WorkflowSetupComponent extends AppComponentBase implements OnInit {
    private _service = inject(WorkflowSetupServiceProxy);

    readonly roleModal = viewChild<CreateWorkflowRoleModalComponent>('roleModal');

    readonly loading = signal(true);
    readonly rows = signal<WorkflowSetupRowDto[]>([]);
    readonly roles = signal<WorkflowSetupRoleDto[]>([]);
    readonly workflowGroupsEnabled = signal(false);

    /** The row being saved, so one Save button spins rather than all of them. */
    readonly savingCode = signal<string | undefined>(undefined);

    /** Codes whose row differs from what was loaded. Drives the Save button and the unsaved marker. */
    readonly dirty = signal<Set<string>>(new Set<string>());

    readonly stepIndexes = [0, 1, 2];

    /** Which cell's role picker is being filled, so a new role can drop straight into it. */
    private _pendingRoleTarget: { code: string; index: number } | undefined;

    readonly approverOptions = [
        { value: WorkflowApproverPersonType.None, key: 'WorkflowSetupNoApprover' },
        { value: WorkflowApproverPersonType.Person, key: 'WorkflowApproverPersonType_Person' },
        { value: WorkflowApproverPersonType.PersonManager, key: 'WorkflowApproverPersonType_PersonManager' },
        { value: WorkflowApproverPersonType.ManagerLevel2, key: 'WorkflowApproverPersonType_ManagerLevel2' },
        { value: WorkflowApproverPersonType.ManagerLevel3, key: 'WorkflowApproverPersonType_ManagerLevel3' },
        { value: WorkflowApproverPersonType.ManagerLevel4, key: 'WorkflowApproverPersonType_ManagerLevel4' },
        { value: WorkflowApproverPersonType.ManagerLevel5, key: 'WorkflowApproverPersonType_ManagerLevel5' },
        { value: WorkflowApproverPersonType.ManagerLevel6, key: 'WorkflowApproverPersonType_ManagerLevel6' },
        { value: WorkflowApproverPersonType.ManagerLevel7, key: 'WorkflowApproverPersonType_ManagerLevel7' },
    ];

    readonly raisedByOptions = [
        { value: WorkflowPersonScope.None, key: 'WorkflowSetupRaisedByAnyone' },
        { value: WorkflowPersonScope.Themselves, key: 'WorkflowPersonScope_Themselves' },
        { value: WorkflowPersonScope.Department, key: 'WorkflowPersonScope_Department' },
        { value: WorkflowPersonScope.SelfSubordinates, key: 'WorkflowPersonScope_SelfSubordinates' },
        { value: WorkflowPersonScope.All, key: 'WorkflowPersonScope_All' },
    ];

    readonly editableCount = computed(() => this.rows().filter((r) => !r.isLocked).length);
    readonly lockedCount = computed(() => this.rows().filter((r) => r.isLocked).length);

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading.set(true);

        this._service
            .getGrid()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                // Pad every row to three cells so the template can index them without guarding. The server
                // ignores empty ones on the way back.
                for (const row of result.rows) {
                    while (row.steps.length < MAX_STEPS) {
                        row.steps.push(
                            new WorkflowSetupStepDto({
                                approverPerson: WorkflowApproverPersonType.None,
                                approvalRequire: ApprovalRequire.Single,
                                workflowRoleId: undefined,
                                workflowRoleName: undefined,
                                currentHolderCount: 0,
                            } as any)
                        );
                    }
                }

                this.rows.set(result.rows);
                this.roles.set(result.roles);
                this.workflowGroupsEnabled.set(result.workflowGroupsEnabled);
                this.dirty.set(new Set<string>());
            });
    }

    // ------------------------------------------------------------------ editing

    isDirty(code: string): boolean {
        return this.dirty().has(code);
    }

    private markDirty(code: string): void {
        const next = new Set(this.dirty());
        next.add(code);
        this.dirty.set(next);
    }

    /** The signal holds the array by reference, so a change inside a row needs a new array to be seen. */
    private touch(code: string): void {
        this.markDirty(code);
        this.rows.set([...this.rows()]);
    }

    setApprover(row: WorkflowSetupRowDto, index: number, value: WorkflowApproverPersonType): void {
        const step = row.steps[index];
        step.approverPerson = +value;

        // A manager position carries no role. Leaving one behind would show in the detailed screens as a
        // setting that does nothing, and would reappear here if the cell were switched back.
        if (step.approverPerson !== WorkflowApproverPersonType.Person) {
            step.workflowRoleId = undefined;
            step.workflowRoleName = undefined;
            step.currentHolderCount = 0;
        }

        this.touch(row.code);
    }

    setRole(row: WorkflowSetupRowDto, index: number, roleId: any): void {
        const step = row.steps[index];
        const id = roleId === '' || roleId === null || roleId === undefined ? undefined : +roleId;

        step.workflowRoleId = id;

        const role = id ? this.roles().find((r) => r.id === id) : undefined;
        step.workflowRoleName = role?.nameEn;
        step.currentHolderCount = role?.currentHolderCount ?? 0;

        this.touch(row.code);
    }

    setApprovalRequire(row: WorkflowSetupRowDto, index: number, value: any): void {
        row.steps[index].approvalRequire = +value;
        this.touch(row.code);
    }

    setField(row: WorkflowSetupRowDto, field: keyof WorkflowSetupRowDto, value: any): void {
        (row as any)[field] = value;
        this.touch(row.code);
    }

    setRaisedBy(row: WorkflowSetupRowDto, value: any): void {
        row.raisedBy = +value;
        this.touch(row.code);
    }

    setEscalation(row: WorkflowSetupRowDto, value: any): void {
        const days = value === '' || value === null || value === undefined ? 0 : +value;
        row.escalateAfterDays = days < 0 ? 0 : days;
        this.touch(row.code);
    }

    /** A step is only a real approver once it says what kind it is. */
    isConfigured(step: WorkflowSetupStepDto): boolean {
        return step.approverPerson !== WorkflowApproverPersonType.None;
    }

    needsRole(step: WorkflowSetupStepDto): boolean {
        return step.approverPerson === WorkflowApproverPersonType.Person;
    }

    /** True when the chosen role currently has nobody in it, which is worth showing in the cell itself. */
    roleIsEmpty(step: WorkflowSetupStepDto): boolean {
        return this.needsRole(step) && !!step.workflowRoleId && step.currentHolderCount === 0;
    }

    // ------------------------------------------------------------------ roles

    openNewRole(row: WorkflowSetupRowDto, index: number): void {
        this._pendingRoleTarget = { code: row.code, index };
        this.roleModal().show();
    }

    onRoleCreated(role: WorkflowSetupRoleDto): void {
        this.roles.set([...this.roles(), role].sort((a, b) => (a.nameEn ?? '').localeCompare(b.nameEn ?? '')));

        const target = this._pendingRoleTarget;
        this._pendingRoleTarget = undefined;

        if (!target) {
            return;
        }

        const row = this.rows().find((r) => r.code === target.code);

        if (!row) {
            return;
        }

        // Drop it straight into the cell that asked for it -- the reason for creating it was to finish
        // that chain, and making them find it again in the list is a step for nothing.
        row.steps[target.index].approverPerson = WorkflowApproverPersonType.Person;
        this.setRole(row, target.index, role.id);
    }

    // ------------------------------------------------------------------ saving

    save(row: WorkflowSetupRowDto): void {
        this.savingCode.set(row.code);

        const input = new SaveWorkflowSetupRowInput();
        input.code = row.code;
        input.steps = row.steps.filter((s) => this.isConfigured(s));
        input.raisedBy = row.raisedBy;
        input.isWorkflowEnabled = row.isWorkflowEnabled;
        input.isEssEnabled = row.isEssEnabled;
        input.allowDirectEntry = row.allowDirectEntry;
        input.notifyByEmail = row.notifyByEmail;
        input.escalateAfterDays = row.escalateAfterDays;
        input.isActive = row.isActive;

        this._service
            .saveRow(input)
            .pipe(finalize(() => this.savingCode.set(undefined)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));

                // Reloaded rather than patched in place: saving re-derives the warnings and can change the
                // lock, and a row showing stale warnings is worse than a moment's flicker.
                this.load();
            });
    }

    isSaving(code: string): boolean {
        return this.savingCode() === code;
    }

    typeName(row: WorkflowSetupRowDto): string {
        return this.isRtlLayout() ? row.nameAr || row.nameEn : row.nameEn || row.nameAr;
    }

    roleName(role: WorkflowSetupRoleDto): string {
        return this.isRtlLayout() ? role.nameAr || role.nameEn : role.nameEn || role.nameAr;
    }

    private isRtlLayout(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
