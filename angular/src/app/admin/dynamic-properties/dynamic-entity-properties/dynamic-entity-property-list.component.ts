import { Component, OnInit, inject, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicEntityPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { SelectAnEntityModalComponent } from '@app/admin/dynamic-properties/select-an-entity-modal.component';
import { Router } from '@angular/router';
import { ManageDynamicEntityPropertyModalComponent } from './manage-dynamic-entity-property-modal.component';

import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { SelectAnEntityModalComponent as SelectAnEntityModalComponent_1 } from '../select-an-entity-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    selector: 'dynamic-entity-property-list',
    templateUrl: './dynamic-entity-property-list.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        SelectAnEntityModalComponent_1,
        ManageDynamicEntityPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class DynamicEntityPropertyListComponent extends AppComponentBase implements OnInit {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    private _router = inject(Router);

    readonly selectAnEntityModal = viewChild<SelectAnEntityModalComponent>('selectAnEntityModal');
    readonly manageDynamicEntityPropertyModalComponent = viewChild<ManageDynamicEntityPropertyModalComponent>(
        'manageDynamicEntityPropertyModalComponent'
    );

    ngOnInit() {
        this.getDynamicEntityProperties();
    }

    getDynamicEntityProperties(): void {
        this.primengTableHelper.showLoadingIndicator();
        this._dynamicEntityPropertyService.getAllEntitiesHasDynamicProperty().subscribe(
            (result) => {
                this.primengTableHelper.totalRecordsCount.set(result.items.length);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            },
            (err) => {
                this.primengTableHelper.hideLoadingIndicator();
            }
        );
    }

    addNewDynamicEntityProperty(): void {
        this.selectAnEntityModal().show();
    }

    gotoEdit(entityFullName: string): void {
        this.manageDynamicEntityPropertyModalComponent().show(entityFullName);
    }
}
