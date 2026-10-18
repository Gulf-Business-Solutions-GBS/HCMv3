import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompetenciesServiceProxy,
    CompetencyDto,
    CreateOrEditKpiInfoDto,
    GetAllCompetenciesInput,
    GetAllKpiCategoriesInput,
    KpiCategoryDto,
    KpiCategoriesServiceProxy,
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
    selector: 'createOrEditKpiInfoModal',
    templateUrl: './create-or-edit-kpi-info-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditKpiInfoModalComponent extends AppComponentBase {
    private _service = inject(KpiInfosServiceProxy);
    private _kpiCategoriesService = inject(KpiCategoriesServiceProxy);
    private _competenciesService = inject(CompetenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly kpiInfo = signal<CreateOrEditKpiInfoDto>(new CreateOrEditKpiInfoDto());
    readonly kpiCategories = signal<KpiCategoryDto[]>([]);
    readonly competencies = signal<CompetencyDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._kpiCategoriesService
            .getAll(new GetAllKpiCategoriesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.kpiCategories.set(result.items));

        this._competenciesService
            .getAll(new GetAllCompetenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, competencyTypeId: undefined }))
            .subscribe((result) => this.competencies.set(result.items));

        if (!id) {
            const record = new CreateOrEditKpiInfoDto();
            record.active = true;
            this.kpiInfo.set(record);
            this.modal().show();
        } else {
            this._service.getKpiInfoForEdit(id).subscribe((result) => {
                this.kpiInfo.set(result.kpiInfo);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiInfo_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.kpiInfo())
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

    updateField(field: keyof CreateOrEditKpiInfoDto, value: any): void {
        const current = this.kpiInfo();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiInfoDto();
        Object.assign(next, current);
        this.kpiInfo.set(next);
    }
}
