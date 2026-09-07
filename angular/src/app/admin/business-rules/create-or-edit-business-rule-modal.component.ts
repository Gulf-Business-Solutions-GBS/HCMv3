import { Component, inject, output, signal, viewChild, computed } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BusinessRuleAssignmentDto,
    BusinessRulesServiceProxy,
    CreateOrEditBusinessRuleAssignmentDto,
    CreateOrEditBusinessRuleDto,
    RuleBaseObjectMetadataDto,
    RuleEventType,
    RuleFunctionMetadataDto,
    RulePurpose,
    TestBusinessRuleInput,
    TestBusinessRuleOutput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { DateTime } from 'luxon';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { RuleExpressionEditorComponent } from './rule-expression-editor.component';
import {
    RuleAction,
    RuleBlock,
    RuleDefinition,
    emptyDefinition,
    newBlock,
    newComparison,
    newConstant,
    pruneDefinition,
} from './rule-model';

/**
 * Authors one rule: its header, its if/then tree, the events it is armed on, and a dry-run panel.
 *
 * The four sit in one dialog on purpose. Authoring a rule and finding out what it does are the same act --
 * splitting the test harness onto another screen is how rule builders end up shipping rules nobody ever
 * ran. Arming stays visually separate (its own tab, its own permission) because that is the step that
 * changes production.
 */
@Component({
    selector: 'createOrEditBusinessRuleModal',
    templateUrl: './create-or-edit-business-rule-modal.component.html',
    styleUrls: ['./create-or-edit-business-rule-modal.component.css'],
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        RuleExpressionEditorComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class CreateOrEditBusinessRuleModalComponent extends AppComponentBase {
    private _service = inject(BusinessRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly testing = signal<boolean>(false);

    readonly record = signal<CreateOrEditBusinessRuleDto>(new CreateOrEditBusinessRuleDto());
    readonly definition = signal<RuleDefinition>(emptyDefinition());
    readonly assignments = signal<BusinessRuleAssignmentDto[]>([]);
    readonly testResult = signal<TestBusinessRuleOutput | undefined>(undefined);

    readonly baseObjects = signal<RuleBaseObjectMetadataDto[]>([]);
    readonly functions = signal<RuleFunctionMetadataDto[]>([]);

    /** Sample values for the dry run, keyed "Parameter.Field". */
    readonly sampleValues = signal<Record<string, string>>({});

    readonly activeTab = signal<'definition' | 'assignments' | 'test'>('definition');

    readonly purposes = [
        { value: RulePurpose.Evaluate, key: 'RulePurposeEvaluate' },
        { value: RulePurpose.Validate, key: 'RulePurposeValidate' },
        { value: RulePurpose.Route, key: 'RulePurposeRoute' },
    ];

    /** The descriptor for the chosen base object. Everything the tree editor offers comes from it. */
    readonly selectedBaseObject = computed<RuleBaseObjectMetadataDto | undefined>(() =>
        this.baseObjects().find((b) => b.code === this.record().baseObject)
    );

    /** Only the events this base object declares -- arming a routing rule on BeforeSave can never work. */
    readonly availableEvents = computed<RuleEventType[]>(() => this.selectedBaseObject()?.supportedEvents ?? []);

    /** Fields a Set action may target: both the parameter and the field must be writable. */
    readonly writableFields = computed<{ parameter: string; field: string; label: string }[]>(() => {
        const baseObject = this.selectedBaseObject();

        if (!baseObject) {
            return [];
        }

        const options: { parameter: string; field: string; label: string }[] = [];

        for (const parameter of baseObject.parameters) {
            for (const field of parameter.fields) {
                if (field.isWritable) {
                    options.push({
                        parameter: parameter.name,
                        field: field.name,
                        label: parameter.displayName + '.' + field.displayName,
                    });
                }
            }
        }

        return options;
    });

    show(id?: number): void {
        this.active.set(true);
        this.testResult.set(undefined);
        this.activeTab.set('definition');
        this.sampleValues.set({});

        this._service.getRuleMetadata().subscribe((metadata) => {
            this.baseObjects.set(metadata.baseObjects);
            this.functions.set(metadata.functions);

            if (!id) {
                const record = new CreateOrEditBusinessRuleDto();
                record.purpose = RulePurpose.Evaluate;
                record.isActive = true;
                record.effectiveFrom = DateTime.now().startOf('day');
                record.baseObject = metadata.baseObjects[0]?.code ?? '';

                this.record.set(record);
                this.definition.set(emptyDefinition());
                this.assignments.set([]);
                this.modal().show();
            } else {
                this._service.getBusinessRuleForEdit(id).subscribe((result) => {
                    this.record.set(result.rule);
                    this.assignments.set(result.assignments);
                    this.definition.set(this.parseDefinition(result.rule.definitionJson));
                    this.modal().show();
                });
            }
        });
    }

    /**
     * A stored definition that will not parse still has to open, or the only screen that could fix it is
     * the one screen that refuses to show it.
     */
    private parseDefinition(json?: string): RuleDefinition {
        if (!json) {
            return emptyDefinition();
        }

        try {
            const parsed = JSON.parse(json) as RuleDefinition;

            return parsed && parsed.blocks && parsed.blocks.length > 0 ? parsed : emptyDefinition();
        } catch {
            this.notify.warn(this.l('RuleDefinitionIsBroken'));
            return emptyDefinition();
        }
    }

    onBaseObjectChanged(): void {
        // Field references belong to the old base object and would every one of them be invalid. Starting
        // clean is honest; silently keeping references that no longer resolve is not.
        this.definition.set(emptyDefinition());
        this.testResult.set(undefined);
        this.sampleValues.set({});
    }

    // ---------------------------------------------------------------------------------------------
    // Blocks and actions
    // ---------------------------------------------------------------------------------------------

    addBlock(): void {
        this.definition.update((d) => ({ ...d, blocks: [...d.blocks, newBlock()] }));
    }

    removeBlock(index: number): void {
        this.definition.update((d) => ({ ...d, blocks: d.blocks.filter((_, i) => i !== index) }));
    }

    /** Turns an unconditional block back into a conditional one, and vice versa. */
    toggleCondition(block: RuleBlock): void {
        block.if = block.if ? null : newComparison();
    }

    addSetAction(block: RuleBlock, branch: 'then' | 'else'): void {
        const first = this.writableFields()[0];

        const action: RuleAction = {
            kind: 'set',
            parameter: first?.parameter ?? '',
            field: first?.field ?? '',
            value: newConstant(),
        };

        this.ensureBranch(block, branch).push(action);
    }

    addErrorAction(block: RuleBlock, branch: 'then' | 'else'): void {
        this.ensureBranch(block, branch).push({ kind: 'error', messageKey: '', args: [] });
    }

    removeAction(block: RuleBlock, branch: 'then' | 'else', index: number): void {
        this.ensureBranch(block, branch).splice(index, 1);
    }

    private ensureBranch(block: RuleBlock, branch: 'then' | 'else'): RuleAction[] {
        if (branch === 'then') {
            block.then = block.then || [];
            return block.then;
        }

        block.else = block.else || [];
        return block.else;
    }

    actionTargetKey(action: RuleAction): string {
        return action.parameter && action.field ? action.parameter + '.' + action.field : '';
    }

    setActionTarget(action: RuleAction, key: string): void {
        const separator = key.indexOf('.');

        if (separator < 0) {
            return;
        }

        action.parameter = key.substring(0, separator);
        action.field = key.substring(separator + 1);
    }

    // ---------------------------------------------------------------------------------------------
    // Assignments
    // ---------------------------------------------------------------------------------------------

    eventLabel(value: RuleEventType): string {
        return this.l('RuleEventType' + RuleEventType[value]);
    }

    addAssignment(): void {
        const events = this.availableEvents();

        if (events.length === 0) {
            return;
        }

        const assignment = new BusinessRuleAssignmentDto();
        assignment.id = 0;
        assignment.businessRuleId = this.record().id ?? 0;
        assignment.eventType = events[0];
        assignment.executionOrder = this.assignments().length + 1;
        assignment.isActive = true;

        this.assignments.update((list) => [...list, assignment]);
    }

    saveAssignment(assignment: BusinessRuleAssignmentDto): void {
        const ruleId = this.record().id;

        if (!ruleId) {
            // Arming needs a rule id, and a rule that has never been saved has none.
            this.notify.warn(this.l('SaveTheRuleBeforeAssigningIt'));
            return;
        }

        const input = new CreateOrEditBusinessRuleAssignmentDto();
        input.id = assignment.id > 0 ? assignment.id : undefined;
        input.businessRuleId = ruleId;
        input.eventType = assignment.eventType;
        input.executionOrder = assignment.executionOrder;
        input.isActive = assignment.isActive;
        input.continueOnError = assignment.continueOnError;
        input.isTraced = assignment.isTraced;

        this._service.createOrEditAssignment(input).subscribe(() => {
            this.notify.success(this.l('SuccessfullySaved'));
            this.reloadAssignments();
        });
    }

    deleteAssignment(assignment: BusinessRuleAssignmentDto): void {
        if (!assignment.id) {
            this.assignments.update((list) => list.filter((a) => a !== assignment));
            return;
        }

        this._service.deleteAssignment(assignment.id).subscribe(() => {
            this.notify.success(this.l('SuccessfullyDeleted'));
            this.reloadAssignments();
        });
    }

    private reloadAssignments(): void {
        const ruleId = this.record().id;

        if (!ruleId) {
            return;
        }

        this._service.getBusinessRuleForEdit(ruleId).subscribe((result) => this.assignments.set(result.assignments));
    }

    // ---------------------------------------------------------------------------------------------
    // Test harness
    // ---------------------------------------------------------------------------------------------

    /** Every field of the base object, so the author can seed whichever ones their rule reads. */
    testableFields(): { key: string; label: string }[] {
        const baseObject = this.selectedBaseObject();

        if (!baseObject) {
            return [];
        }

        const fields: { key: string; label: string }[] = [];

        for (const parameter of baseObject.parameters) {
            for (const field of parameter.fields) {
                fields.push({
                    key: parameter.name + '.' + field.name,
                    label: parameter.displayName + '.' + field.displayName,
                });
            }
        }

        return fields;
    }

    setSampleValue(key: string, value: string): void {
        this.sampleValues.update((values) => ({ ...values, [key]: value }));
    }

    runTest(): void {
        this.testing.set(true);

        const input = new TestBusinessRuleInput();
        input.baseObject = this.record().baseObject;
        input.purpose = this.record().purpose;
        input.definitionJson = JSON.stringify(pruneDefinition(this.definition()));
        input.sampleValues = this.sampleValues();
        input.asOfDate = DateTime.now().startOf('day');

        this._service
            .testBusinessRule(input)
            .pipe(finalize(() => this.testing.set(false)))
            .subscribe((result) => this.testResult.set(result));
    }

    // ---------------------------------------------------------------------------------------------

    save(): void {
        this.saving.set(true);

        const record = this.record();
        record.definitionJson = JSON.stringify(pruneDefinition(this.definition()));

        this._service
            .createOrEditBusinessRule(record)
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

    /**
     * The date inputs hand back "YYYY-MM-DD" strings while the DTO carries Luxon DateTimes. Converting here
     * rather than in the template keeps the date library out of the view, where it cannot be type-checked.
     */
    setEffectiveFrom(value: string): void {
        this.updateField('effectiveFrom', value ? DateTime.fromISO(value) : undefined);
    }

    setEffectiveTo(value: string): void {
        this.updateField('effectiveTo', value ? DateTime.fromISO(value) : undefined);
    }

    /**
     * Signals compare by reference, so mutating the DTO in place would not notify anything bound to it.
     * Copying onto a fresh instance is the house pattern for editing a proxy DTO held in a signal.
     */
    updateField(field: keyof CreateOrEditBusinessRuleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;

        const next = new CreateOrEditBusinessRuleDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
