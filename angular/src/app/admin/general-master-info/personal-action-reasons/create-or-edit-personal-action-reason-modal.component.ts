import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPersonalActionReasonDto,
    GetAllNotificationConfigsInput,
    GetAllPersonalActionsInput,
    NotificationConfigDto,
    NotificationConfigsServiceProxy,
    PersonalActionDto,
    PersonalActionReasonsServiceProxy,
    PersonalActionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPersonalActionReasonModal',
    templateUrl: './create-or-edit-personal-action-reason-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPersonalActionReasonModalComponent extends AppComponentBase {
    private _personalActionReasonsService = inject(PersonalActionReasonsServiceProxy);
    private _personalActionsService = inject(PersonalActionsServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly personalActionReason = signal<CreateOrEditPersonalActionReasonDto>(
        new CreateOrEditPersonalActionReasonDto()
    );
    readonly personalActions = signal<PersonalActionDto[]>([]);
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    show(personalActionReasonId?: number): void {
        this.active.set(true);

        this._personalActionsService
            .getAll(
                new GetAllPersonalActionsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.personalActions.set(result.items);
            });

        this._notificationConfigsService
            .getAll(
                new GetAllNotificationConfigsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.notificationConfigs.set(result.items));

        if (!personalActionReasonId) {
            this.personalActionReason.set(new CreateOrEditPersonalActionReasonDto());
            this.modal().show();
        } else {
            this._personalActionReasonsService
                .getPersonalActionReasonForEdit(personalActionReasonId)
                .subscribe((result) => {
                    this.personalActionReason.set(result.personalActionReason);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('PersonalActionReason_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._personalActionReasonsService
            .createOrEdit(this.personalActionReason())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditPersonalActionReasonDto, value: any): void {
        const current = this.personalActionReason();
        (current as any)[field] = value;
        const next = new CreateOrEditPersonalActionReasonDto();
        Object.assign(next, current);
        this.personalActionReason.set(next);
    }
}
