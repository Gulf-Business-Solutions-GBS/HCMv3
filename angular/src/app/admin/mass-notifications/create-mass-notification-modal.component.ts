import { Component, inject, output, viewChild, signal } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UserLookupTableModalComponent } from './user-lookup-table-modal.component';
import { OrganizationUnitLookupTableModalComponent } from './organization-unit-lookup-table-modal.component';
import {
    CreateMassNotificationInput,
    MassNotificationOrganizationUnitLookupTableDto,
    MassNotificationUserLookupTableDto,
    NotificationServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'create-mass-notification-modal',
    templateUrl: './create-mass-notification-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        UserLookupTableModalComponent,
        OrganizationUnitLookupTableModalComponent,
        LocalizePipe,
    ],
})
export class CreateMassNotificationModalComponent extends AppComponentBase {
    private _notificationServiceProxy = inject(NotificationServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly userLookupTableModalComponent = viewChild<UserLookupTableModalComponent>('userLookupTableModalComponent');
    readonly organizationUnitLookupTableModalComponent = viewChild<OrganizationUnitLookupTableModalComponent>(
        'organizationUnitLookupTableModalComponent'
    );

    readonly modalSave = output<any>();

    active = signal(false);
    saving = signal(false);

    createMassNotificationInput = signal<CreateMassNotificationInput>(new CreateMassNotificationInput());

    userNames = signal('');
    organizationUnitDisplayNames = signal('');
    notifiers = signal<string[]>([]);
    showSMSTargetNotifierMessage = signal(false);

    show(): void {
        this.createMassNotificationInput.set(new CreateMassNotificationInput());
        this.active.set(true);

        this._notificationServiceProxy.getAllNotifiers().subscribe((result) => {
            this.notifiers.set(result);
        });

        this.modal().show();
    }

    save(): void {
        const input = this.createMassNotificationInput();
        if (!input.message || input.message === '') {
            abp.message.error(this.l('MassNotificationMessageFieldIsRequiredMessage'));
            return;
        }

        if (!input.targetNotifiers || input.targetNotifiers.length === 0) {
            abp.message.error(this.l('MassNotificationTargetNotifiersFieldIsRequiredMessage'));
            return;
        }

        if (
            (!input.userIds || input.userIds.length === 0) &&
            (!input.organizationUnitIds || input.organizationUnitIds.length === 0)
        ) {
            abp.message.error(this.l('MassNotificationUserOrOrganizationUnitFieldIsRequiredMessage'));
            return;
        }

        this.saving.set(true);

        this._notificationServiceProxy
            .createMassNotification(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    openSelectUserModal() {
        this.userLookupTableModalComponent().show();
    }

    openSelectOrganizationUnitModal() {
        this.organizationUnitLookupTableModalComponent().show();
    }

    setUserIdNull() {
        this.createMassNotificationInput.update((input) => {
            const updated = new CreateMassNotificationInput();
            updated.init({ ...input.toJSON(), userIds: [] });
            return updated;
        });
        this.userNames.set('');
    }

    setOrganizationUnitIdNull() {
        this.createMassNotificationInput.update((input) => {
            const updated = new CreateMassNotificationInput();
            updated.init({ ...input.toJSON(), organizationUnitIds: [] });
            return updated;
        });
        this.organizationUnitDisplayNames.set('');
    }

    onUsersSelected(selectedItems: MassNotificationUserLookupTableDto[]) {
        this.createMassNotificationInput.update((input) => {
            const updated = new CreateMassNotificationInput();
            updated.init({ ...input.toJSON(), userIds: selectedItems.map((i) => i.id) });
            return updated;
        });
        this.userNames.set(
            '[ ' +
                selectedItems.length +
                ' ' +
                this.l('ItemsSelected') +
                '] ' +
                selectedItems.map((i) => i.displayName).join(', ')
        );
    }

    onOrganizationUnitsSelected(selectedItems: MassNotificationOrganizationUnitLookupTableDto[]) {
        this.createMassNotificationInput.update((input) => {
            const updated = new CreateMassNotificationInput();
            updated.init({ ...input.toJSON(), organizationUnitIds: selectedItems.map((i) => i.id) });
            return updated;
        });
        this.organizationUnitDisplayNames.set(
            '[ ' +
                selectedItems.length +
                ' ' +
                this.l('ItemsSelected') +
                '] ' +
                selectedItems.map((i) => i.displayName).join(', ')
        );
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    onTargetNotifiersChanged(): void {
        const input = this.createMassNotificationInput();
        this.showSMSTargetNotifierMessage.set(
            input.targetNotifiers.includes('HCMv3.Notifications.SmsRealTimeNotifier')
        );
    }
}
