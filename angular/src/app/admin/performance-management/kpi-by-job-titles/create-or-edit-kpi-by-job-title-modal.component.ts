import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditKpiByJobTitleDto,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    KpiByJobTitlesServiceProxy,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditKpiByJobTitleModal',
    templateUrl: './create-or-edit-kpi-by-job-title-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditKpiByJobTitleModalComponent extends AppComponentBase {
    private _service = inject(KpiByJobTitlesServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly kpiByJobTitle = signal<CreateOrEditKpiByJobTitleDto>(new CreateOrEditKpiByJobTitleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'KpiByJobTitle',
            record: this.kpiByJobTitle,
            isNew: this.isNew,
            keys: () => ({ JobTitleId: this.kpiByJobTitle().jobTitleId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly jobTitles = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._jobTitlesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobTitles.set(result.items));

        if (!id) {
            this.kpiByJobTitle.set(new CreateOrEditKpiByJobTitleDto());
            this.modal().show();
        } else {
            this._service.getKpiByJobTitleForEdit(id).subscribe((result) => {
                this.kpiByJobTitle.set(result.kpiByJobTitle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiByJobTitle_JobTitleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.kpiByJobTitle())
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

    updateField(field: keyof CreateOrEditKpiByJobTitleDto, value: any): void {
        const current = this.kpiByJobTitle();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiByJobTitleDto();
        Object.assign(next, current);
        this.kpiByJobTitle.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
