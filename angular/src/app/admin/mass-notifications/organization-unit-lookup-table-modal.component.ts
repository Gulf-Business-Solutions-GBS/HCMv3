import { Component, inject, output, signal, viewChild } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NameValueDto,
    OrganizationUnitServiceProxy,
    OrganizationUnitDto,
} from '@shared/service-proxies/service-proxies';
import {
    IOrganizationUnitsTreeComponentData,
    OrganizationUnitsTreeComponent,
} from '../shared/organization-unit-tree.component';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'organization-unit-lookup-table-modal',
    templateUrl: './organization-unit-lookup-table-modal.component.html',
    imports: [AppBsModalDirective, OrganizationUnitsTreeComponent, ButtonBusyDirective, LocalizePipe],
})
export class OrganizationUnitLookupTableModalComponent extends AppComponentBase {
    private _organizationUnitServiceProxy = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('organizationUnitLookupTableModal');
    readonly organizationUnitTree = viewChild<OrganizationUnitsTreeComponent>('organizationUnitTree');

    readonly modalSave = output<NameValueDto[]>();

    readonly filterText = signal<string>('');
    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly allOrganizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly organizationUnitData = signal<IOrganizationUnitsTreeComponentData>(undefined);

    show(): void {
        this.active.set(true);
        this.modal().show();
        this.getOrganizationUnits();
    }

    getOrganizationUnits(): void {
        this._organizationUnitServiceProxy.getAll().subscribe((result) => {
            this.allOrganizationUnits.set(result);
        });
    }

    onShown(): void {
        this.organizationUnitData.set({
            allOrganizationUnits: this.allOrganizationUnits(),
            selectedOrganizationUnits: [],
        });
    }

    save() {
        this.active.set(false);
        this.modal().hide();
        this.modalSave.emit(this.organizationUnitTree().getSelectedOrganizations());
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
