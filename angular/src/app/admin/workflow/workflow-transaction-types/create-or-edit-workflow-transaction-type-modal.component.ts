import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkflowTransactionTypeDto,
    GetAllRequestMastersInput,
    RequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowRequestAction,
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

    /** The per-action chains: a change, a cancellation and a return may each be routed differently. */
    private readonly allActionChains: {
        id: string;
        field: keyof CreateOrEditWorkflowTransactionTypeDto;
        labelKey: string;
        action: WorkflowRequestAction;
    }[] = [
        { id: 'AmendmentRequestMasterId', field: 'amendmentRequestMasterId', labelKey: 'AmendmentChain', action: WorkflowRequestAction.Amendment },
        { id: 'CancellationRequestMasterId', field: 'cancellationRequestMasterId', labelKey: 'CancellationChain', action: WorkflowRequestAction.Cancellation },
        { id: 'ReturnRequestMasterId', field: 'returnRequestMasterId', labelKey: 'ReturnChain', action: WorkflowRequestAction.Return },
    ];

    /**
     * The chains this type can use. Leave has all three; a passport withdrawal only a return; an asset
     * request none -- and a field for something the type cannot do would only invite a wrong setup.
     */
    readonly actionChains = computed(() => this.allActionChains.filter((chain) => this.supportedChangeActions().includes(chain.action)));

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
                } as any)
            );
            this.supportedChangeActions.set([]);
            this.refreshWarning();
            this.modal().show();
        } else {
            this._service.getWorkflowTransactionTypeForEdit(id).subscribe((result) => {
                this.record.set(result.workflowTransactionType);
                this.supportedChangeActions.set(result.supportedChangeActions ?? []);
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

    private refreshWarning(): void {
        const record = this.record();

        this.isEnabledWithoutChain.set(!!record.isWorkflowEnabled && !record.defaultRequestMasterId);
    }
}
