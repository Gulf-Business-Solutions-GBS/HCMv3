import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterDailyDutyDto,
    DailyDutiesServiceProxy,
    DailyDutyDto,
    GetAllDailyDutiesInput,
    GetAllRosterDutySchedulesInput,
    RosterDailyDutiesServiceProxy,
    RosterDutyScheduleDto,
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
    selector: 'createOrEditRosterDailyDutyModal',
    templateUrl: './create-or-edit-roster-daily-duty-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterDailyDutyModalComponent extends AppComponentBase {
    private _rosterDailyDutiesService = inject(RosterDailyDutiesServiceProxy);
    private _rosterDutySchedulesService = inject(RosterDutySchedulesServiceProxy);
    private _dailyDutiesService = inject(DailyDutiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterDailyDuty = signal<CreateOrEditRosterDailyDutyDto>(new CreateOrEditRosterDailyDutyDto());
    readonly rosterDutySchedules = signal<RosterDutyScheduleDto[]>([]);
    readonly dailyDuties = signal<DailyDutyDto[]>([]);

    show(rosterDailyDutyId?: number): void {
        this.active.set(true);

        this._rosterDutySchedulesService
            .getAll(new GetAllRosterDutySchedulesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.rosterDutySchedules.set(result.items);
            });

        this._dailyDutiesService
            .getAll(new GetAllDailyDutiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.dailyDuties.set(result.items);
            });

        if (!rosterDailyDutyId) {
            this.rosterDailyDuty.set(new CreateOrEditRosterDailyDutyDto());
            this.modal().show();
        } else {
            this._rosterDailyDutiesService.getRosterDailyDutyForEdit(rosterDailyDutyId).subscribe((result) => {
                this.rosterDailyDuty.set(result.rosterDailyDuty);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterDailyDuty_CalendarDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterDailyDutiesService
            .createOrEdit(this.rosterDailyDuty())
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

    updateField(field: keyof CreateOrEditRosterDailyDutyDto, value: any): void {
        const current = this.rosterDailyDuty();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterDailyDutyDto();
        Object.assign(next, current);
        this.rosterDailyDuty.set(next);
    }

    updateDateField(field: keyof CreateOrEditRosterDailyDutyDto, value: string): void {
        const current = this.rosterDailyDuty();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditRosterDailyDutyDto();
        Object.assign(next, current);
        this.rosterDailyDuty.set(next);
    }
}
