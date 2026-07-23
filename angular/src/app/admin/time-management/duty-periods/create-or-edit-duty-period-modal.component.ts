import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditDutyPeriodDto,
    DutiesServiceProxy,
    DutyDto,
    DutyPeriodsServiceProxy,
    GetAllDutiesInput,
    GetAllHolidayCalendarsInput,
    HolidayCalendarDto,
    HolidayCalendarsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditDutyPeriodModal',
    templateUrl: './create-or-edit-duty-period-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDutyPeriodModalComponent extends AppComponentBase {
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);
    private _holidayCalendarsService = inject(HolidayCalendarsServiceProxy);
    private _dutiesService = inject(DutiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly dutyPeriod = signal<CreateOrEditDutyPeriodDto>(new CreateOrEditDutyPeriodDto());
    readonly holidayCalendars = signal<HolidayCalendarDto[]>([]);
    readonly duties = signal<DutyDto[]>([]);

    show(dutyPeriodId?: number): void {
        this.active.set(true);

        this._holidayCalendarsService
            .getAll(new GetAllHolidayCalendarsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.holidayCalendars.set(result.items);
            });

        this._dutiesService
            .getAll(new GetAllDutiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.duties.set(result.items);
            });

        if (!dutyPeriodId) {
            this.dutyPeriod.set(new CreateOrEditDutyPeriodDto());
            this.modal().show();
        } else {
            this._dutyPeriodsService.getDutyPeriodForEdit(dutyPeriodId).subscribe((result) => {
                this.dutyPeriod.set(result.dutyPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DutyPeriod_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._dutyPeriodsService
            .createOrEdit(this.dutyPeriod())
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

    updateField(field: keyof CreateOrEditDutyPeriodDto, value: any): void {
        const current = this.dutyPeriod();
        (current as any)[field] = value;
        const next = new CreateOrEditDutyPeriodDto();
        Object.assign(next, current);
        this.dutyPeriod.set(next);
    }

    updateDateField(field: keyof CreateOrEditDutyPeriodDto, value: string): void {
        const current = this.dutyPeriod();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditDutyPeriodDto();
        Object.assign(next, current);
        this.dutyPeriod.set(next);
    }
}
