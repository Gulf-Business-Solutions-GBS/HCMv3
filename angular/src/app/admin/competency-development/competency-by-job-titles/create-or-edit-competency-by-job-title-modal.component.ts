import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompetencyByJobTitlesServiceProxy,
    CreateOrEditCompetencyByJobTitleDto,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditCompetencyByJobTitleModal',
    templateUrl: './create-or-edit-competency-by-job-title-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCompetencyByJobTitleModalComponent extends AppComponentBase {
    private _service = inject(CompetencyByJobTitlesServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly competencyByJobTitle = signal<CreateOrEditCompetencyByJobTitleDto>(new CreateOrEditCompetencyByJobTitleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'CompetencyByJobTitle',
            record: this.competencyByJobTitle,
            isNew: this.isNew,
            keys: () => ({ JobTitleId: this.competencyByJobTitle().jobTitleId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly jobTitles = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        if (!id) {
            this.competencyByJobTitle.set(new CreateOrEditCompetencyByJobTitleDto());
            this.modal().show();
        } else {
            this._service.getCompetencyByJobTitleForEdit(id).subscribe((result) => {
                this.competencyByJobTitle.set(result.competencyByJobTitle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CompetencyByJobTitle_JobTitleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.competencyByJobTitle())
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

    updateField(field: keyof CreateOrEditCompetencyByJobTitleDto, value: any): void {
        const current = this.competencyByJobTitle();
        (current as any)[field] = value;
        const next = new CreateOrEditCompetencyByJobTitleDto();
        Object.assign(next, current);
        this.competencyByJobTitle.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateEndDate(value: string): void {
        this.updateField('endDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
