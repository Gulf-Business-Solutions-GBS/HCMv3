import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditKpiByOrgUnitDetailDto,
    GetAllKpiByOrgUnitsInput,
    GetAllKpiInfosInput,
    KpiByOrgUnitDetailsServiceProxy,
    KpiByOrgUnitDto,
    KpiByOrgUnitsServiceProxy,
    KpiInfoDto,
    KpiInfosServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditKpiByOrgUnitDetailModal',
    templateUrl: './create-or-edit-kpi-by-org-unit-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditKpiByOrgUnitDetailModalComponent extends AppComponentBase {
    private _service = inject(KpiByOrgUnitDetailsServiceProxy);
    private _kpiByOrgUnitsService = inject(KpiByOrgUnitsServiceProxy);
    private _kpiInfosService = inject(KpiInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditKpiByOrgUnitDetailDto>(new CreateOrEditKpiByOrgUnitDetailDto());
    readonly kpiByOrgUnits = signal<KpiByOrgUnitDto[]>([]);
    readonly kpiInfos = signal<KpiInfoDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._kpiByOrgUnitsService
            .getAll(
                new GetAllKpiByOrgUnitsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    organizationUnitId: undefined,
                })
            )
            .subscribe((result) => this.kpiByOrgUnits.set(result.items));

        this._kpiInfosService
            .getAll(
                new GetAllKpiInfosInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    kpiCategoryId: undefined,
                })
            )
            .subscribe((result) => this.kpiInfos.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditKpiByOrgUnitDetailDto());
            this.modal().show();
        } else {
            this._service.getKpiByOrgUnitDetailForEdit(id).subscribe((result) => {
                this.record.set(result.kpiByOrgUnitDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiByOrgUnitDetail_KpiByOrgUnitId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    kpiByOrgUnitLabel(item: KpiByOrgUnitDto): string {
        return `${item.organizationUnitName} (${item.startDate?.toISODate()})`;
    }

    updateField(field: keyof CreateOrEditKpiByOrgUnitDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiByOrgUnitDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
