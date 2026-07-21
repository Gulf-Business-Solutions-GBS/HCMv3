import { Component, inject, output, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicEntityPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { CreateDynamicEntityPropertyModalComponent } from './create-dynamic-entity-property-modal.component';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    selector: 'manage-dynamic-entity-property-modal',
    templateUrl: './manage-dynamic-entity-property-modal.component.html',
    imports: [
        AppBsModalDirective,
        BusyIfDirective,
        TableModule,
        CreateDynamicEntityPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class ManageDynamicEntityPropertyModalComponent extends AppComponentBase {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);

    readonly onPropertyChange = output<any>();

    readonly createDynamicEntityPropertyModal = viewChild<CreateDynamicEntityPropertyModalComponent>(
        'createDynamicEntityPropertyModal'
    );
    readonly modal = viewChild<ModalDirective>('createModal');

    entityFullName = signal<string>('');

    show(entityFullName: string): void {
        this.entityFullName.set(entityFullName);
        this.getDynamicEntityProperties();
        this.modal().show();
    }

    handlePropertyChanges(): void {
        this.onPropertyChange.emit(null);
        this.getDynamicEntityProperties();
    }

    getDynamicEntityProperties(): void {
        this.primengTableHelper.showLoadingIndicator();
        this._dynamicEntityPropertyService.getAllPropertiesOfAnEntity(this.entityFullName()).subscribe(
            (result) => {
                this.primengTableHelper.totalRecordsCount.set(result.items.length);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
                this.hideMainSpinner();
            },
            (err) => {
                this.hideMainSpinner();
            }
        );
    }

    addNewDynamicEntityProperty(): void {
        this.createDynamicEntityPropertyModal().show(this.entityFullName());
    }

    deleteDynamicEntityProperty(id: number): void {
        this.message.confirm(this.l('DeleteDynamicPropertyMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dynamicEntityPropertyService.delete(id).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.handlePropertyChanges();
                });
            }
        });
    }

    close(): void {
        this.modal().hide();
    }
}
