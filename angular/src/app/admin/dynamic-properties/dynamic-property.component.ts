import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DynamicPropertyDto, DynamicPropertyServiceProxy } from '@shared/service-proxies/service-proxies';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { CreateOrEditDynamicPropertyModalComponent } from './create-or-edit-dynamic-property-modal.component';
import { InputTypeConfigurationService } from '@app/shared/common/input-types/input-type-configuration.service';
import { DynamicPropertyValueModalComponent } from '@app/admin/dynamic-properties/dynamic-property-value/dynamic-property-value-modal.component';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { DynamicEntityPropertyListComponent } from './dynamic-entity-properties/dynamic-entity-property-list.component';
import { DynamicPropertyValueModalComponent as DynamicPropertyValueModalComponent_1 } from './dynamic-property-value/dynamic-property-value-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './dynamic-property.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        DynamicEntityPropertyListComponent,
        DynamicPropertyValueModalComponent_1,
        CreateOrEditDynamicPropertyModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class DynamicPropertyComponent extends AppComponentBase {
    private _dynamicPropertyService = inject(DynamicPropertyServiceProxy);
    private _inputTypeConfigurationService = inject(InputTypeConfigurationService);

    readonly createOrEditDynamicPropertyModal =
        viewChild<CreateOrEditDynamicPropertyModalComponent>('createOrEditDynamicProperty');
    readonly dynamicPropertyValueModal = viewChild<DynamicPropertyValueModalComponent>('dynamicPropertyValueModal');

    readonly activeDynamicPropertyMenuItems = signal<MenuItem[]>([]);

    showDynamicPropertyMenu(event: Event, record: DynamicPropertyDto, menu: TieredMenu): void {
        this.activeDynamicPropertyMenuItems.set(this.getMenuItemsForDynamicProperty(record));
        menu.toggle(event);
    }

    getMenuItemsForDynamicProperty(record: DynamicPropertyDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.isGranted('Pages.Administration.DynamicProperties.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.editDynamicProperty(record.id),
            });
        }

        if (this.isGranted('Pages.Administration.DynamicPropertyValue.Edit') && this.hasValues(record.inputType)) {
            items.push({
                label: this.l('EditValues'),
                icon: 'pi pi-list',
                command: () => this.editValues(record),
            });
        }

        if (this.isGranted('Pages.Administration.DynamicProperties.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteDynamicProperty(record.id),
            });
        }

        return items;
    }

    getDynamicProperties(): void {
        this.primengTableHelper.showLoadingIndicator();
        this._dynamicPropertyService.getAll().subscribe(
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

    addNewDynamicProperty(): void {
        this.createOrEditDynamicPropertyModal().show();
    }

    editDynamicProperty(dynamicPropertyId: number): void {
        this.createOrEditDynamicPropertyModal().show(dynamicPropertyId);
    }

    deleteDynamicProperty(dynamicPropertyId: number): void {
        this.message.confirm(this.l('DeleteDynamicPropertyMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dynamicPropertyService.delete(dynamicPropertyId).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.getDynamicProperties();
                });
            }
        });
    }

    editValues(dynamicProperty: DynamicPropertyDto): void {
        this.dynamicPropertyValueModal().show(dynamicProperty.id);
    }

    hasValues(inputType: string): boolean {
        return this._inputTypeConfigurationService.getByName(inputType).hasValues;
    }
}
