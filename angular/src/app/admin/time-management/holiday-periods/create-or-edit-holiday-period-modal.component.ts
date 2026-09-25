import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditHolidayPeriodDto,
    GetAllHolidaysInput,
    HolidayDto,
    HolidayPeriodsServiceProxy,
    HolidaysServiceProxy,
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
    selector: 'createOrEditHolidayPeriodModal',
    templateUrl: './create-or-edit-holiday-period-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditHolidayPeriodModalComponent extends AppComponentBase {
    private _holidayPeriodsService = inject(HolidayPeriodsServiceProxy);
    private _holidaysService = inject(HolidaysServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly holidayPeriod = signal<CreateOrEditHolidayPeriodDto>(new CreateOrEditHolidayPeriodDto());
    readonly holidays = signal<HolidayDto[]>([]);

    show(holidayPeriodId?: number): void {
        this.active.set(true);

        this._holidaysService
            .getAll(new GetAllHolidaysInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, holidayCalendarId: undefined }))
            .subscribe((result) => {
                this.holidays.set(result.items);
            });

        if (!holidayPeriodId) {
            this.holidayPeriod.set(new CreateOrEditHolidayPeriodDto());
            this.modal().show();
        } else {
            this._holidayPeriodsService.getHolidayPeriodForEdit(holidayPeriodId).subscribe((result) => {
                this.holidayPeriod.set(result.holidayPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('HolidayPeriod_HolidayId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._holidayPeriodsService
            .createOrEdit(this.holidayPeriod())
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

    updateField(field: keyof CreateOrEditHolidayPeriodDto, value: any): void {
        const current = this.holidayPeriod();
        (current as any)[field] = value;
        const next = new CreateOrEditHolidayPeriodDto();
        Object.assign(next, current);
        this.holidayPeriod.set(next);
    }

    updateDateField(field: keyof CreateOrEditHolidayPeriodDto, value: string): void {
        const current = this.holidayPeriod();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditHolidayPeriodDto();
        Object.assign(next, current);
        this.holidayPeriod.set(next);
    }
}
