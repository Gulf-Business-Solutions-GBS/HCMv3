import { Component, inject, output, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DynamicEntityPropertyDto,
    DynamicEntityPropertyServiceProxy,
    DynamicPropertyServiceProxy,
    DynamicPropertyDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { forkJoin } from 'rxjs';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'create-dynamic-entity-property-modal',
    templateUrl: './create-dynamic-entity-property-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateDynamicEntityPropertyModalComponent extends AppComponentBase {
    private _dynamicEntityPropertyService = inject(DynamicEntityPropertyServiceProxy);
    private _dynamicPropertyService = inject(DynamicPropertyServiceProxy);

    private entityFullName = signal<string>('');

    readonly modalSave = output<any>();
    readonly modal = viewChild<ModalDirective>('createModal');
    dynamicEntityProperty = signal<DynamicEntityPropertyDto>(new DynamicEntityPropertyDto());
    allDynamicProperties = signal<DynamicPropertyDto[]>([]);
    initialized = signal(false);
    saving = signal(false);

    show(entityFullName: string): void {
        this.entityFullName.set(entityFullName);
        this.initialize();
        const newProperty = new DynamicEntityPropertyDto();
        newProperty.entityFullName = entityFullName;
        newProperty.dynamicPropertyId = undefined;
        this.dynamicEntityProperty.set(newProperty);
        this.modal().show();
    }

    save(): void {
        this.saving.set(true);
        this.showMainSpinner();

        this.dynamicEntityProperty.update((property) => {
            const updated = new DynamicEntityPropertyDto();
            updated.init({ ...property.toJSON(), tenantId: abp.session.tenantId });
            return updated;
        });

        this._dynamicEntityPropertyService.add(this.dynamicEntityProperty()).subscribe(
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

    close(): void {
        this.modal().hide();
    }

    private initialize() {
        this.initialized.set(false);
        let definedParametersObservable = this._dynamicEntityPropertyService.getAllPropertiesOfAnEntity(
            this.entityFullName()
        );
        let allParametersObservable = this._dynamicPropertyService.getAll();
        this.showMainSpinner();
        forkJoin([allParametersObservable, definedParametersObservable]).subscribe(
            ([dynamicProperties, definedParameters]) => {
                this.allDynamicProperties.set(
                    dynamicProperties.items.filter(function (element) {
                        return definedParameters.items.map((item) => item.dynamicPropertyId).indexOf(element.id) === -1;
                    })
                );
                this.hideMainSpinner();
                this.initialized.set(true);
            },
            (err) => {
                this.hideMainSpinner();
            }
        );
    }
}
