import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditNotificationMasterDto,
    EntityDto,
    GetAllNotificationConfigsInput,
    NotificationConfigDto,
    NotificationConfigsServiceProxy,
    NotificationMastersServiceProxy,
    NotificationRulePreviewDto,
    NotificationRuleSourceDto,
    NotificationRulesServiceProxy,
    NotificationRuleSubTypeDto,
    PreviewNotificationRuleInput,
    RuleFunctionMetadataDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';
import { RuleFormulaInputComponent } from '../../business-rules/rule-formula-input.component';
import { parseOptionalFormula, printFormula } from '../../business-rules/rule-formula';
import { RuleExpression } from '../../business-rules/rule-model';

/**
 * A Notification Master rule: a type from the whitelisted list, an optional sub type that really filters,
 * and a criterion typed in the business rule formula editor.
 *
 * The criterion is stored as the formula editor's expression JSON -- the same tree the business rule engine
 * evaluates -- so this screen owns no grammar of its own. The text in the box is only a view of it.
 *
 * "Preview" asks the server how many records the rule would notify about today, before anyone switches it
 * on: a broad rule can address hundreds of people on its first run. For a saved rule the preview also says
 * how many it has not sent yet, and "Mark these as sent" logs them without sending -- the go-live answer to
 * old records all firing on day one.
 */
@Component({
    selector: 'createOrEditNotificationMasterModal',
    templateUrl: './create-or-edit-notification-master-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
        RuleFormulaInputComponent,
    ],
})
export class CreateOrEditNotificationMasterModalComponent extends AppComponentBase {
    private _service = inject(NotificationMastersServiceProxy);
    private _rulesService = inject(NotificationRulesServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditNotificationMasterDto>(new CreateOrEditNotificationMasterDto());
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    readonly sources = signal<NotificationRuleSourceDto[]>([]);
    readonly functions = signal<RuleFunctionMetadataDto[]>([]);
    readonly subTypes = signal<NotificationRuleSubTypeDto[]>([]);

    /** What the formula box shows; the record holds the parsed tree as JSON. */
    readonly formulaText = signal<string>('');
    readonly formulaValid = signal<boolean>(true);

    readonly preview = signal<NotificationRulePreviewDto | undefined>(undefined);
    readonly previewing = signal<boolean>(false);
    readonly marking = signal<boolean>(false);

    readonly selectedSource = computed(() => this.sources().find((s) => s.code === this.record().notificationType));

    private metadataLoaded = false;

    show(id?: number): void {
        this.active.set(true);
        this.preview.set(undefined);
        this.formulaValid.set(true);

        this._notificationConfigsService
            .getAll(new GetAllNotificationConfigsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.notificationConfigs.set(result.items));

        if (!this.metadataLoaded) {
            this._rulesService.getNotificationRuleMetadata().subscribe((metadata) => {
                this.sources.set(metadata.sources ?? []);
                this.functions.set(metadata.functions ?? []);
                this.metadataLoaded = true;
            });
        }

        if (!id) {
            this.record.set(new CreateOrEditNotificationMasterDto());
            this.formulaText.set('');
            this.subTypes.set([]);
            this.modal().show();
        } else {
            this._service.getNotificationMasterForEdit(id).subscribe((result) => {
                this.record.set(result.notificationMaster);
                this.formulaText.set(this.toFormulaText(result.notificationMaster.criterion));
                this.loadSubTypes(result.notificationMaster.notificationType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationMaster_NotificationType')?.focus();
    }

    onTypeChange(code: string): void {
        this.updateField('notificationType', code);

        // A sub type id means something different in every type's list, so it cannot survive a type change.
        this.updateField('notificationSubTypeId', undefined);
        this.preview.set(undefined);
        this.loadSubTypes(code);
    }

    onFormulaChange(text: string): void {
        this.formulaText.set(text);
        this.preview.set(undefined);

        try {
            const expression = parseOptionalFormula(text);
            this.updateField('criterion', expression ? JSON.stringify(expression) : undefined);
        } catch {
            // The formula box shows the error and reports itself invalid, which blocks Save.
        }
    }

    runPreview(): void {
        const record = this.record();
        if (!record.notificationType) {
            return;
        }

        this.previewing.set(true);
        this._rulesService
            .previewNotificationRule(
                new PreviewNotificationRuleInput({
                    notificationType: record.notificationType,
                    notificationSubTypeId: record.notificationSubTypeId,
                    criterion: record.criterion,
                    notificationMasterId: record.id,
                })
            )
            .pipe(finalize(() => this.previewing.set(false)))
            .subscribe((result) => this.preview.set(result));
    }

    markExistingAsSent(): void {
        const id = this.record().id;
        if (!id) {
            return;
        }

        this.message.confirm(this.l('NotificationRuleMarkExistingWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.marking.set(true);
            this._rulesService
                .markExistingAsSent(new EntityDto({ id }))
                .pipe(finalize(() => this.marking.set(false)))
                .subscribe((count) => {
                    this.notify.success(this.l('NotificationRuleMarkedAsSent', count));
                    this.runPreview();
                });
        });
    }

    save(): void {
        if (!this.formulaValid()) {
            return;
        }

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

    updateField(field: keyof CreateOrEditNotificationMasterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditNotificationMasterDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    private loadSubTypes(code: string | undefined): void {
        this.subTypes.set([]);

        if (!code) {
            return;
        }

        this._rulesService.getSubTypes(code).subscribe((items) => this.subTypes.set(items ?? []));
    }

    /**
     * The stored tree printed back as a formula. A criterion saved before the rule engine is legacy XAF text,
     * not JSON: it is shown as it is, so the box flags it and the administrator can see what to re-enter.
     */
    private toFormulaText(criterion: string | undefined): string {
        if (!criterion) {
            return '';
        }

        try {
            return printFormula(JSON.parse(criterion) as RuleExpression);
        } catch {
            return criterion;
        }
    }
}
