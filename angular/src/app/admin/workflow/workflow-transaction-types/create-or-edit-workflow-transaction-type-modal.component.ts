import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkflowTransactionTypeDto,
    GetAllRequestMastersInput,
    RequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowRequestAction,
    WorkflowSubTypeOptionDto,
    WorkflowTransactionTypeRuleDto,
    WorkflowTransactionTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * One row of the transaction catalog.
 *
 * <p>The chain dropdown is the point of this dialog. A type with workflow switched on and no chain cannot be
 * routed at all -- the submit fails rather than recording quietly -- so the two fields are shown together
 * and the screen says what the combination means rather than leaving an administrator to find out by
 * submitting a request.</p>
 */
@Component({
    selector: 'createOrEditWorkflowTransactionTypeModal',
    templateUrl: './create-or-edit-workflow-transaction-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkflowTransactionTypeModalComponent extends AppComponentBase {
    private _service = inject(WorkflowTransactionTypesServiceProxy);
    private _requestMastersService = inject(RequestMastersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkflowTransactionTypeDto>(new CreateOrEditWorkflowTransactionTypeDto());
    readonly requestMasters = signal<RequestMasterDto[]>([]);

    /**
     * Which changes can be raised against an approved transaction of the type being edited, as the server
     * reports it. Empty for most types, and for a type that is being created.
     */
    readonly supportedChangeActions = signal<WorkflowRequestAction[]>([]);

    /** The tables a sub type can come from (absence types, document types ...), for the dropdown. */
    readonly subTypeSources = signal<string[]>([]);

    /** The rows of the chosen sub type source, so a rule can name "Sick Leave" rather than an id. */
    readonly subTypeOptions = signal<WorkflowSubTypeOptionDto[]>([]);

    /**
     * The actions a rule may name: a new request always, plus whatever the type allows to be raised against
     * an approved transaction -- leave has amendment, cancellation and return; most types have none.
     */
    readonly actionOptions = computed(() => {
        const all = [
            { value: WorkflowRequestAction.New, key: 'WorkflowRequestActionNew' },
            { value: WorkflowRequestAction.Amendment, key: 'WorkflowRequestActionAmendment' },
            { value: WorkflowRequestAction.Cancellation, key: 'WorkflowRequestActionCancellation' },
            { value: WorkflowRequestAction.Return, key: 'WorkflowRequestActionReturn' },
        ];

        return all.filter(
            (o) => o.value === WorkflowRequestAction.New || this.supportedChangeActions().includes(o.value)
        );
    });

    /** "AbsenceType" -> "Absence Type", for the source dropdown. */
    sourceLabel(source: string): string {
        return source.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2');
    }

    /**
     * True when this type will be refused at submit time: routing is on, but there is no chain to route to.
     *
     * <p>Shown as a warning in the dialog rather than blocked, because the two fields are often set in
     * either order and forbidding the intermediate state would make the form fight the person filling it
     * in. Saying so is enough.</p>
     */
    readonly isEnabledWithoutChain = signal<boolean>(false);

    show(id?: number): void {
        this.active.set(true);

        this._requestMastersService
            .getAll(
                new GetAllRequestMastersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.requestMasters.set(result.items));

        this._service.getSubTypeSources().subscribe((sources) => this.subTypeSources.set(sources));

        if (!id) {
            // Active by default; workflow and ESS off. Registering a type must not start routing live
            // transactions through approval the moment it is saved -- the same reasoning the seeder uses.
            // DisplayOrder is spelled out because the column is NOT NULL: leaving it undefined sends no
            // value at all rather than a zero.
            this.record.set(
                new CreateOrEditWorkflowTransactionTypeDto({
                    isActive: true,
                    isWorkflowEnabled: false,
                    isEssEnabled: false,
                    displayOrder: 0,
                    rules: [],
                } as any)
            );
            this.subTypeOptions.set([]);
            this.supportedChangeActions.set([]);
            this.refreshWarning();
            this.modal().show();
        } else {
            this._service.getWorkflowTransactionTypeForEdit(id).subscribe((result) => {
                this.record.set(result.workflowTransactionType);
                this.supportedChangeActions.set(result.supportedChangeActions ?? []);
                this.loadSubTypeOptions(result.workflowTransactionType.subTypeSource);
                this.refreshWarning();
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkflowTransactionType_Code')?.focus();
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

    updateField(field: keyof CreateOrEditWorkflowTransactionTypeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;

        // A new instance, not a mutation: the signal compares by reference, so editing in place would leave
        // everything downstream -- including the warning below -- reading stale values.
        const next = new CreateOrEditWorkflowTransactionTypeDto();
        Object.assign(next, current);
        this.record.set(next);

        this.refreshWarning();
    }

    /** A new source means different sub types: reload them, and clear rule sub types that no longer apply. */
    changeSubTypeSource(source: string): void {
        const rules = (this.record().rules ?? []).map((rule) => {
            const copy = new WorkflowTransactionTypeRuleDto();
            Object.assign(copy, rule);
            copy.requestSubTypeId = undefined;
            return copy;
        });

        this.updateField('subTypeSource', source || undefined);
        this.updateField('rules', rules);
        this.loadSubTypeOptions(source);
    }

    addRule(): void {
        this.updateField('rules', [...(this.record().rules ?? []), new WorkflowTransactionTypeRuleDto()]);
    }

    removeRule(index: number): void {
        const rules = [...(this.record().rules ?? [])];
        rules.splice(index, 1);
        this.updateField('rules', rules);
    }

    updateRule(index: number, field: keyof WorkflowTransactionTypeRuleDto, value: any): void {
        const rules = [...(this.record().rules ?? [])];
        const rule = new WorkflowTransactionTypeRuleDto();
        Object.assign(rule, rules[index]);
        (rule as any)[field] = value ?? undefined;
        rules[index] = rule;
        this.updateField('rules', rules);
    }

    /** True when every rule names a chain -- the one thing a rule cannot do without. */
    rulesComplete(): boolean {
        return (this.record().rules ?? []).every((rule) => !!rule.requestMasterId);
    }

    private loadSubTypeOptions(source: string | undefined): void {
        if (!source) {
            this.subTypeOptions.set([]);
            return;
        }

        this._service.getSubTypeOptions(source).subscribe((options) => this.subTypeOptions.set(options));
    }

    private refreshWarning(): void {
        const record = this.record();

        this.isEnabledWithoutChain.set(!!record.isWorkflowEnabled && !record.defaultRequestMasterId);
    }
}
