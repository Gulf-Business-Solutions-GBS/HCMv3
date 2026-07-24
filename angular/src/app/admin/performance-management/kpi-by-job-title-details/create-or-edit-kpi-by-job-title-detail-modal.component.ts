import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditKpiByJobTitleDetailDto,
    GetAllKpiByJobTitlesInput,
    GetAllKpiInfosInput,
    KpiByJobTitleDetailsServiceProxy,
    KpiByJobTitleDto,
    KpiByJobTitlesServiceProxy,
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
    selector: 'createOrEditKpiByJobTitleDetailModal',
    templateUrl: './create-or-edit-kpi-by-job-title-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditKpiByJobTitleDetailModalComponent extends AppComponentBase {
    private _service = inject(KpiByJobTitleDetailsServiceProxy);
    private _kpiByJobTitlesService = inject(KpiByJobTitlesServiceProxy);
    private _kpiInfosService = inject(KpiInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditKpiByJobTitleDetailDto>(new CreateOrEditKpiByJobTitleDetailDto());
    readonly kpiByJobTitles = signal<KpiByJobTitleDto[]>([]);
    readonly kpiInfos = signal<KpiInfoDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._kpiByJobTitlesService
            .getAll(new GetAllKpiByJobTitlesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, jobTitleId: undefined }))
            .subscribe((result) => this.kpiByJobTitles.set(result.items));

        this._kpiInfosService
            .getAll(new GetAllKpiInfosInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, kpiCategoryId: undefined }))
            .subscribe((result) => this.kpiInfos.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditKpiByJobTitleDetailDto());
            this.modal().show();
        } else {
            this._service.getKpiByJobTitleDetailForEdit(id).subscribe((result) => {
                this.record.set(result.kpiByJobTitleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiByJobTitleDetail_KpiByJobTitleId')?.focus();
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

    kpiByJobTitleLabel(item: KpiByJobTitleDto): string {
        return `${item.jobTitleName} (${item.startDate?.toISODate()})`;
    }

    updateField(field: keyof CreateOrEditKpiByJobTitleDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiByJobTitleDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
