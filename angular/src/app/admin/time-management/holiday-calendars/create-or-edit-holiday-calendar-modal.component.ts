import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditHolidayCalendarDto, HolidayCalendarsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditHolidayCalendarModal',
    templateUrl: './create-or-edit-holiday-calendar-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditHolidayCalendarModalComponent extends AppComponentBase {
    private _holidayCalendarsService = inject(HolidayCalendarsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly holidayCalendar = signal<CreateOrEditHolidayCalendarDto>(new CreateOrEditHolidayCalendarDto());

    show(holidayCalendarId?: number): void {
        this.active.set(true);

        if (!holidayCalendarId) {
            this.holidayCalendar.set(new CreateOrEditHolidayCalendarDto());
            this.modal().show();
        } else {
            this._holidayCalendarsService.getHolidayCalendarForEdit(holidayCalendarId).subscribe((result) => {
                this.holidayCalendar.set(result.holidayCalendar);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('HolidayCalendar_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._holidayCalendarsService
            .createOrEdit(this.holidayCalendar())
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

    updateField(field: keyof CreateOrEditHolidayCalendarDto, value: any): void {
        const current = this.holidayCalendar();
        (current as any)[field] = value;
        const next = new CreateOrEditHolidayCalendarDto();
        Object.assign(next, current);
        this.holidayCalendar.set(next);
    }
}
