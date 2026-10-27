import { Component, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ManagerComponent } from '@app/admin/dynamic-properties/dynamic-entity-properties/value/manager.component';
import { AppBsModalDirective } from '../../../../../shared/common/appBsModal/app-bs-modal.directive';

import { ManagerComponent as ManagerComponent_1 } from './manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'manage-dynamic-entity-property-values-modal',
    templateUrl: './manage-values-modal.component.html',
    imports: [AppBsModalDirective, ManagerComponent_1, LocalizePipe],
})
export class ManageValuesModalComponent extends AppComponentBase {
    readonly dynamicEntityPropertyValueManager = viewChild<ManagerComponent>('dynamicEntityPropertyValueManager');
    readonly modal = viewChild<ModalDirective>('manageDynamicEntityParameterValuesModal');

    entityFullName = signal<string>('');
    entityId = signal<string>('');

    initialized = signal(false);

    saveAll(): void {
        this.dynamicEntityPropertyValueManager().saveAll();
    }

    close(): void {
        this.modal().hide();
    }

    show(entityFullName: string, entityId: string) {
        this.entityFullName.set(entityFullName);
        this.entityId.set(entityId);

        const dynamicEntityPropertyValueManager = this.dynamicEntityPropertyValueManager();
        if (dynamicEntityPropertyValueManager) {
            dynamicEntityPropertyValueManager.initialize();
        }

        this.initialized.set(true);
        this.modal().show();
    }
}
