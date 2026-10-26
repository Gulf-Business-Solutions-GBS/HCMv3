import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CDAnnualYearReviewsServiceProxy,
    CDRatingDto,
    CDRatingsServiceProxy,
    CompetenciesServiceProxy,
    CompetencyDto,
    CreateOrEditCDAnnualYearReviewDto,
    GetAllCDRatingsInput,
    GetAllCompetenciesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCDAnnualYearReviewModal',
    templateUrl: './create-or-edit-cd-annual-year-review-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCDAnnualYearReviewModalComponent extends AppComponentBase {
    private _service = inject(CDAnnualYearReviewsServiceProxy);
    private _competenciesService = inject(CompetenciesServiceProxy);
    private _cdRatingsService = inject(CDRatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCDAnnualYearReviewDto>(new CreateOrEditCDAnnualYearReviewDto());
    readonly competencies = signal<CompetencyDto[]>([]);
    readonly cdRatings = signal<CDRatingDto[]>([]);

    show(cdFormId: number, id?: number): void {
        this.active.set(true);

        this._competenciesService
            .getAll(
                new GetAllCompetenciesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    competencyTypeId: undefined,
                })
            )
            .subscribe((result) => this.competencies.set(result.items));

        this._cdRatingsService
            .getAll(
                new GetAllCDRatingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.cdRatings.set(result.items));

        if (!id) {
            const record = new CreateOrEditCDAnnualYearReviewDto();
            record.cdFormId = cdFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCDAnnualYearReviewForEdit(id).subscribe((result) => {
                this.record.set(result.cdAnnualYearReview);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CDAnnualYearReview_CompetencyId')?.focus();
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

    updateField(field: keyof CreateOrEditCDAnnualYearReviewDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCDAnnualYearReviewDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
