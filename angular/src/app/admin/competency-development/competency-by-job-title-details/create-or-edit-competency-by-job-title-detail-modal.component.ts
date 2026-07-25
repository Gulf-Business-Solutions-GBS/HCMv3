import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompetenciesServiceProxy,
    CompetencyByJobTitleDetailsServiceProxy,
    CompetencyByJobTitleDto,
    CompetencyByJobTitlesServiceProxy,
    CompetencyDto,
    CreateOrEditCompetencyByJobTitleDetailDto,
    GetAllCompetenciesInput,
    GetAllCompetencyByJobTitlesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCompetencyByJobTitleDetailModal',
    templateUrl: './create-or-edit-competency-by-job-title-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCompetencyByJobTitleDetailModalComponent extends AppComponentBase {
    private _service = inject(CompetencyByJobTitleDetailsServiceProxy);
    private _competencyByJobTitlesService = inject(CompetencyByJobTitlesServiceProxy);
    private _competenciesService = inject(CompetenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCompetencyByJobTitleDetailDto>(new CreateOrEditCompetencyByJobTitleDetailDto());
    readonly competencyByJobTitles = signal<CompetencyByJobTitleDto[]>([]);
    readonly competencies = signal<CompetencyDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._competencyByJobTitlesService
            .getAll(new GetAllCompetencyByJobTitlesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, jobTitleId: undefined }))
            .subscribe((result) => this.competencyByJobTitles.set(result.items));

        this._competenciesService
            .getAll(new GetAllCompetenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, competencyTypeId: undefined }))
            .subscribe((result) => this.competencies.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditCompetencyByJobTitleDetailDto());
            this.modal().show();
        } else {
            this._service.getCompetencyByJobTitleDetailForEdit(id).subscribe((result) => {
                this.record.set(result.competencyByJobTitleDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CompetencyByJobTitleDetail_CompetencyByJobTitleId')?.focus();
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

    competencyByJobTitleLabel(item: CompetencyByJobTitleDto): string {
        return `${item.jobTitleName} (${item.startDate?.toISODate()})`;
    }

    updateField(field: keyof CreateOrEditCompetencyByJobTitleDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCompetencyByJobTitleDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
