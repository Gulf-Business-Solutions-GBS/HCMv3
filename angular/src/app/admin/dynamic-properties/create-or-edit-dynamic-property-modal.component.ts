import { Component, OnInit, inject, output, viewChild, signal } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DynamicPropertyServiceProxy,
    DynamicPropertyDto,
    DynamicEntityPropertyDefinitionServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { PermissionTreeModalComponent } from '@app/admin/shared/permission-tree-modal.component';

import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { PermissionTreeModalComponent as PermissionTreeModalComponent_1 } from '../shared/permission-tree-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'create-or-edit-dynamic-property-modal',
    templateUrl: './create-or-edit-dynamic-property-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, PermissionTreeModalComponent_1, LocalizePipe],
})
export class CreateOrEditDynamicPropertyModalComponent extends AppComponentBase implements OnInit {
    private _dynamicPropertyService = inject(DynamicPropertyServiceProxy);
    private _dynamicEntityPropertyDefinitionServiceProxy = inject(DynamicEntityPropertyDefinitionServiceProxy);

    readonly modalSave = output<any>();
    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly permissionFilterTreeModal = viewChild<PermissionTreeModalComponent>('permissionFilterTreeModal');

    dynamicProperty = signal<DynamicPropertyDto>(new DynamicPropertyDto());
    allIputTypes = signal<string[]>([]);
    dynamicPropertyId = signal<number | undefined>(undefined);
    active = signal(false);
    loading = signal(true);
    saving = signal(false);

    public show(dynamicPropertyId?: number): void {
        this.dynamicPropertyId.set(dynamicPropertyId);
        if (!dynamicPropertyId) {
            this.dynamicProperty.set(new DynamicPropertyDto());
            this.active.set(true);
            this.modal().show();
            return;
        }

        this.showMainSpinner();
        this._dynamicPropertyService.get(dynamicPropertyId).subscribe(
            (result) => {
                this.dynamicProperty.set(result);
                this.hideMainSpinner();
                this.active.set(true);
                this.modal().show();
            },
            (e) => {
                this.hideMainSpinner();
            }
        );
    }

    save(): void {
        this.saving.set(true);
        const property = this.dynamicProperty();
        let observable: Observable<void>;
        if (!property.id) {
            observable = this._dynamicPropertyService.add(property);
        } else {
            observable = this._dynamicPropertyService.update(property);
        }

        this.showMainSpinner();
        observable.subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.hideMainSpinner();
                this.modalSave.emit(null);
                this.modal().hide();
                this.saving.set(false);
            },
            (e) => {
                this.hideMainSpinner();
                this.saving.set(false);
            }
        );
    }

    ngOnInit(): void {
        this._dynamicEntityPropertyDefinitionServiceProxy.getAllAllowedInputTypeNames().subscribe((data) => {
            this.allIputTypes.set(data);
            this.loading.set(false);
        });
    }

    close(): void {
        this.modal().hide();
    }

    openPermissionTreeModal(): void {
        this.permissionFilterTreeModal().openPermissionTreeModal();
    }

    onPermissionSelected(selectedValues: string[]): void {
        this.dynamicProperty.update((property) => {
            const updated = new DynamicPropertyDto();
            updated.init({ ...property.toJSON(), permission: selectedValues[0] });
            return updated;
        });
    }
}
