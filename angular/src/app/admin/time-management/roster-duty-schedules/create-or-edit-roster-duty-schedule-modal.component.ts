import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterDutyScheduleDto,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutyPeriodsInput,
    GetAllHolidayCalendarsInput,
    HolidayCalendarDto,
    HolidayCalendarsServiceProxy,
    RosterDutySchedulesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditRosterDutyScheduleModal',
    templateUrl: './create-or-edit-roster-duty-schedule-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterDutyScheduleModalComponent extends AppComponentBase {
    private _rosterDutySchedulesService = inject(RosterDutySchedulesServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);
    private _holidayCalendarsService = inject(HolidayCalendarsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterDutySchedule = signal<CreateOrEditRosterDutyScheduleDto>(new CreateOrEditRosterDutyScheduleDto());
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);
    readonly holidayCalendars = signal<HolidayCalendarDto[]>([]);

    show(rosterDutyScheduleId?: number): void {
        this.active.set(true);

        this._dutyPeriodsService
            .getAll(
                new GetAllDutyPeriodsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.dutyPeriods.set(result.items);
            });

        this._holidayCalendarsService
            .getAll(
                new GetAllHolidayCalendarsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.holidayCalendars.set(result.items);
            });

        if (!rosterDutyScheduleId) {
            this.rosterDutySchedule.set(new CreateOrEditRosterDutyScheduleDto());
            this.modal().show();
        } else {
            this._rosterDutySchedulesService.getRosterDutyScheduleForEdit(rosterDutyScheduleId).subscribe((result) => {
                this.rosterDutySchedule.set(result.rosterDutySchedule);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterDutySchedule_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterDutySchedulesService
            .createOrEdit(this.rosterDutySchedule())
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

    updateField(field: keyof CreateOrEditRosterDutyScheduleDto, value: any): void {
        const current = this.rosterDutySchedule();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterDutyScheduleDto();
        Object.assign(next, current);
        this.rosterDutySchedule.set(next);
    }

    updateDateField(field: keyof CreateOrEditRosterDutyScheduleDto, value: string): void {
        const current = this.rosterDutySchedule();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditRosterDutyScheduleDto();
        Object.assign(next, current);
        this.rosterDutySchedule.set(next);
    }
}
