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
    SaveWorkflowSetupDefaultChainInput,
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

/** Days before an unanswered request escalates, on a transaction nobody has configured yet. */
const DEFAULT_ESCALATION_DAYS = 3;

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

    /** True while "Save all" is working through the unsaved rows. */
    readonly savingAll = signal(false);

    /** Codes whose row differs from what was loaded. Drives the Save button and the unsaved marker. */
    readonly dirty = signal<Set<string>>(new Set<string>());

    readonly stepIndexes = [0, 1, 2];

    /** Which cell's role picker is being filled, so a new role can drop straight into it. */
    private _pendingRoleTarget: { code: string | undefined; index: number } | undefined;

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
        { value: WorkflowApproverPersonType.PersonOnRequest, key: 'WorkflowApproverPersonType_PersonOnRequest' },
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

    /** How many rows are waiting to be saved, for the Save all button. */
    readonly unsavedCount = computed(() => this.rows().filter((r) => !r.isLocked && this.dirty().has(r.code)).length);

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
                        row.steps.push(this.emptyStep());
                    }
                }

                const defaults = [...(result.defaultChainSteps ?? [])];
                while (defaults.length < MAX_STEPS) {
                    defaults.push(this.emptyStep());
                }

                this.defaultSteps.set(defaults);
                this.defaultUsedByCount.set(result.defaultChainUsedByCount);
                // Only adopt the stored values once a route actually exists. A route nobody has defined
                // yet reports email off and no escalation, and letting that overwrite the proposed
                // starting values would mean the screen suggests something different on a fresh tenant
                // than it does a second later.
                const routeExists = (result.defaultChainSteps ?? []).length > 0;

                if (routeExists) {
                    this.defaultNotifyByEmail.set(result.defaultChainNotifyByEmail);
                    this.defaultEscalateAfterDays.set(result.defaultChainEscalateAfterDays);
                }
                this.defaultDirty.set(false);

                this.rows.set(result.rows);
                this.roles.set(result.roles);
                this.workflowGroupsEnabled.set(result.workflowGroupsEnabled);
                this.dirty.set(new Set<string>());

                this.proposeDefaultsForUntouchedRows();
            });
    }

    /**
     * Fills in the usual answers on transactions nobody has configured yet, so a new tenant is one Save
     * away from working rather than thirty.
     *
     * <p>Only touches rows that are switched off AND have no approvers of their own -- a transaction
     * somebody has already set up keeps exactly what they set. The values are proposed, not saved: each
     * row is marked unsaved and "Save all" applies them, so nothing changes behind your back.</p>
     */
    private proposeDefaultsForUntouchedRows(): void {
        if (!this.hasDefaultChain()) {
            // Without a route to follow, "use default" is an offer that cannot be kept -- the server
            // refuses it. The rest of the defaults wait until the route exists, so the screen does not
            // propose a half-configuration that fails on save.
            return;
        }

        const touched = new Set(this.dirty());

        for (const row of this.rows()) {
            if (row.isLocked) {
                continue;
            }

            const alreadyConfigured = row.isWorkflowEnabled || row.steps.some((s) => this.isConfigured(s));

            if (alreadyConfigured) {
                continue;
            }

            row.usesDefaultChain = true;
            row.isWorkflowEnabled = true;
            row.isEssEnabled = true;
            row.allowDirectEntry = true;
            row.notifyByEmail = true;
            row.isActive = true;
            row.escalateAfterDays = DEFAULT_ESCALATION_DAYS;

            row.steps = this.defaultSteps().map(
                (s) =>
                    new WorkflowSetupStepDto({
                        approverPerson: s.approverPerson,
                        requestPersonField: s.requestPersonField,
                        workflowRoleId: s.workflowRoleId,
                        workflowRoleName: s.workflowRoleName,
                        approvalRequire: s.approvalRequire,
                        currentHolderCount: s.currentHolderCount,
                    } as any)
            );

            while (row.steps.length < MAX_STEPS) {
                row.steps.push(this.emptyStep());
            }

            touched.add(row.code);
        }

        this.dirty.set(touched);
        this.rows.set([...this.rows()]);
    }

    /** Saves every row with unsaved changes, one after another. */
    saveAll(): void {
        const pending = this.rows().filter((r) => !r.isLocked && this.isDirty(r.code));

        if (pending.length === 0) {
            return;
        }

        this.savingAll.set(true);

        // Sequentially rather than in parallel: each save re-derives the locks and warnings from the whole
        // catalog, and thirty of those at once is how a grid ends up showing one row's answer on another.
        const next = (index: number): void => {
            if (index >= pending.length) {
                this.savingAll.set(false);
                this.notify.info(this.l('SavedSuccessfully'));
                this.load();
                return;
            }

            this._service.saveRow(this.toInput(pending[index])).subscribe({
                next: () => next(index + 1),
                error: () => {
                    // Stops at the first refusal instead of pressing on: the rest are probably the same
                    // problem, and thirty identical error toasts help nobody.
                    this.savingAll.set(false);
                    this.load();
                },
            });
        };

        next(0);
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

        // The field of a "Person on Request" step is chosen on the detailed step screen; the grid only carries it.
        // Any other kind of step names no field.
        if (step.approverPerson !== WorkflowApproverPersonType.PersonOnRequest) {
            step.requestPersonField = undefined;
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



    /** A step in one readable phrase, for the collapsed default-chain summary and the read-only cells. */
    describeStep(step: WorkflowSetupStepDto): string {
        const option = this.approverOptions.find((o) => o.value === step.approverPerson);
        const kind = this.l(option?.key ?? 'WorkflowSetupNoApprover');

        if (step.approverPerson !== WorkflowApproverPersonType.Person) {
            return kind;
        }

        // The role IS the answer for a Person step; saying "Person (by role)" without naming it tells the
        // reader nothing they did not already know.
        return step.workflowRoleName || this.l('WorkflowSetupChooseARole');
    }
    // ------------------------------------------------------------------ the default chain

    readonly defaultSteps = signal<WorkflowSetupStepDto[]>([]);
    readonly defaultUsedByCount = signal(0);
    readonly defaultDirty = signal(false);

    /**
     * Email and escalation belong to the ROUTE, not the transaction: they are written onto its steps, so
     * every transaction following the default necessarily shares them. Editing them here rather than
     * per-row is what makes that honest.
     */
    readonly defaultNotifyByEmail = signal(true);
    readonly defaultEscalateAfterDays = signal(DEFAULT_ESCALATION_DAYS);
    readonly savingDefault = signal(false);
    readonly defaultPanelOpen = signal(false);

    readonly hasDefaultChain = computed(() => this.defaultSteps().some((s) => this.isConfigured(s)));

    /** Names of the locked transactions, so the banner says which rather than only how many. */
    readonly lockedNames = computed(() =>
        this.rows()
            .filter((r) => r.isLocked)
            .map((r) => this.typeName(r))
            .join(', ')
    );

    toggleDefaultPanel(): void {
        this.defaultPanelOpen.set(!this.defaultPanelOpen());
    }


    setDefaultNotifyByEmail(value: boolean): void {
        this.defaultNotifyByEmail.set(value);
        this.defaultDirty.set(true);
    }

    setDefaultEscalation(value: any): void {
        const days = value === '' || value === null || value === undefined ? 0 : +value;
        this.defaultEscalateAfterDays.set(days < 0 ? 0 : days);
        this.defaultDirty.set(true);
    }
    setDefaultApprover(index: number, value: WorkflowApproverPersonType): void {
        const step = this.defaultSteps()[index];
        step.approverPerson = +value;

        if (step.approverPerson !== WorkflowApproverPersonType.Person) {
            step.workflowRoleId = undefined;
            step.workflowRoleName = undefined;
            step.currentHolderCount = 0;
        }

        this.defaultSteps.set([...this.defaultSteps()]);
        this.defaultDirty.set(true);
    }

    setDefaultRole(index: number, roleId: any): void {
        const step = this.defaultSteps()[index];
        const id = roleId === '' || roleId === null || roleId === undefined ? undefined : +roleId;

        step.workflowRoleId = id;

        const role = id ? this.roles().find((r) => r.id === id) : undefined;
        step.workflowRoleName = role?.nameEn;
        step.currentHolderCount = role?.currentHolderCount ?? 0;

        this.defaultSteps.set([...this.defaultSteps()]);
        this.defaultDirty.set(true);
    }

    saveDefaultChain(): void {
        this.savingDefault.set(true);

        const input = new SaveWorkflowSetupDefaultChainInput();
        input.steps = this.defaultSteps().filter((s) => this.isConfigured(s));
        input.notifyByEmail = this.defaultNotifyByEmail();
        input.escalateAfterDays = this.defaultEscalateAfterDays();

        this._service
            .saveDefaultChain(input)
            .pipe(finalize(() => this.savingDefault.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));

                // Reloaded rather than patched: every row following the default has just changed, and
                // showing them stale is exactly the confusion a shared chain invites.
                this.load();
            });
    }

    /** Switching a row on or off the default changes what its cells mean, so the whole row is redrawn. */
    setUsesDefault(row: WorkflowSetupRowDto, value: boolean): void {
        row.usesDefaultChain = value;

        if (value) {
            // Show what it will actually do straight away rather than after a save.
            row.steps = this.defaultSteps().map(
                (s) =>
                    new WorkflowSetupStepDto({
                        approverPerson: s.approverPerson,
                        workflowRoleId: s.workflowRoleId,
                        workflowRoleName: s.workflowRoleName,
                        approvalRequire: s.approvalRequire,
                        currentHolderCount: s.currentHolderCount,
                    } as any)
            );

            while (row.steps.length < MAX_STEPS) {
                row.steps.push(this.emptyStep());
            }
        }

        this.touch(row.code);
    }

    private emptyStep(): WorkflowSetupStepDto {
        return new WorkflowSetupStepDto({
            approverPerson: WorkflowApproverPersonType.None,
            approvalRequire: ApprovalRequire.Single,
            workflowRoleId: undefined,
            workflowRoleName: undefined,
            currentHolderCount: 0,
        } as any);
    }
    // ------------------------------------------------------------------ roles


    /**
     * Same as the row version, but for the default route's own cells. A code of undefined is what tells
     * the handler the new role belongs to the default rather than to a transaction.
     */
    openNewRoleForDefault(index: number): void {
        this._pendingRoleTarget = { code: undefined, index };
        this.roleModal().show();
    }
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

        // Drop it straight into the cell that asked for it -- the reason for creating it was to finish
        // that route, and making them find it again in a list is a step for nothing.
        if (target.code === undefined) {
            this.setDefaultApprover(target.index, WorkflowApproverPersonType.Person);
            this.setDefaultRole(target.index, role.id);
            return;
        }

        const row = this.rows().find((r) => r.code === target.code);

        if (!row) {
            return;
        }

        row.steps[target.index].approverPerson = WorkflowApproverPersonType.Person;
        this.setRole(row, target.index, role.id);
    }


    // ------------------------------------------------------------------ saving

    /** One row as the server wants it. Shared by Save and Save all, so the two cannot drift apart. */
    private toInput(row: WorkflowSetupRowDto): SaveWorkflowSetupRowInput {
        const input = new SaveWorkflowSetupRowInput();
        input.code = row.code;
        input.useDefaultChain = row.usesDefaultChain;
        input.steps = row.usesDefaultChain ? [] : row.steps.filter((s) => this.isConfigured(s));
        input.raisedBy = row.raisedBy;
        input.isWorkflowEnabled = row.isWorkflowEnabled;
        input.isEssEnabled = row.isEssEnabled;
        input.allowDirectEntry = row.allowDirectEntry;
        input.notifyByEmail = row.notifyByEmail;
        input.escalateAfterDays = row.escalateAfterDays;
        input.isActive = row.isActive;

        return input;
    }

    save(row: WorkflowSetupRowDto): void {
        this.savingCode.set(row.code);

        const input = this.toInput(row);
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
