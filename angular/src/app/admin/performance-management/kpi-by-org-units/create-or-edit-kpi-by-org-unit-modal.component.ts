import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditKpiByOrgUnitDto,
    KpiByOrgUnitsServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditKpiByOrgUnitModal',
    templateUrl: './create-or-edit-kpi-by-org-unit-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditKpiByOrgUnitModalComponent extends AppComponentBase {
    private _service = inject(KpiByOrgUnitsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly kpiByOrgUnit = signal<CreateOrEditKpiByOrgUnitDto>(new CreateOrEditKpiByOrgUnitDto());
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._organizationUnitService.getOrganizationUnits().subscribe((result) => this.organizationUnits.set(result.items ?? []));

        if (!id) {
            this.kpiByOrgUnit.set(new CreateOrEditKpiByOrgUnitDto());
            this.modal().show();
        } else {
            this._service.getKpiByOrgUnitForEdit(id).subscribe((result) => {
                this.kpiByOrgUnit.set(result.kpiByOrgUnit);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiByOrgUnit_OrganizationUnitId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.kpiByOrgUnit())
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

    updateField(field: keyof CreateOrEditKpiByOrgUnitDto, value: any): void {
        const current = this.kpiByOrgUnit();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiByOrgUnitDto();
        Object.assign(next, current);
        this.kpiByOrgUnit.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
