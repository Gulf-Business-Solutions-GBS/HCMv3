import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkLocationDutyPeriodDto,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutyPeriodsInput,
    GetAllWorkLocationsInput,
    WorkLocationDto,
    WorkLocationDutyPeriodsServiceProxy,
    WorkLocationsServiceProxy,
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
    selector: 'createOrEditWorkLocationDutyPeriodModal',
    templateUrl: './create-or-edit-work-location-duty-period-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkLocationDutyPeriodModalComponent extends AppComponentBase {
    private _service = inject(WorkLocationDutyPeriodsServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkLocationDutyPeriodDto>(new CreateOrEditWorkLocationDutyPeriodDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'WorkLocationDutyPeriod',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ WorkLocationId: this.record().workLocationId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly workLocations = signal<WorkLocationDto[]>([]);
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditWorkLocationDutyPeriodDto());
            this.modal().show();
        } else {
            this._service.getWorkLocationDutyPeriodForEdit(id).subscribe((result) => {
                this.record.set(result.workLocationDutyPeriod);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._workLocationsService
            .getAll(new GetAllWorkLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workLocations.set(result.items));

        this._dutyPeriodsService
            .getAll(new GetAllDutyPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.dutyPeriods.set(result.items));
    }

    onShown(): void {
        document.getElementById('WorkLocationDutyPeriod_WorkLocationId')?.focus();
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

    updateField(field: keyof CreateOrEditWorkLocationDutyPeriodDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkLocationDutyPeriodDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
