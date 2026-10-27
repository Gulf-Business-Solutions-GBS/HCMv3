import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditHolidayDto,
    GetAllHolidayCalendarsInput,
    GetAllOvertimeTypesInput,
    HolidayCalendarDto,
    HolidayCalendarsServiceProxy,
    HolidaysServiceProxy,
    OvertimeTypeDto,
    OvertimeTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditHolidayModal',
    templateUrl: './create-or-edit-holiday-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditHolidayModalComponent extends AppComponentBase {
    private _holidaysService = inject(HolidaysServiceProxy);
    private _holidayCalendarsService = inject(HolidayCalendarsServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly holiday = signal<CreateOrEditHolidayDto>(new CreateOrEditHolidayDto());
    readonly holidayCalendars = signal<HolidayCalendarDto[]>([]);
    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    show(holidayId?: number): void {
        this.active.set(true);

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

        this._overtimeTypesService
            .getAll(
                new GetAllOvertimeTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.overtimeTypes.set(result.items);
            });

        if (!holidayId) {
            this.holiday.set(new CreateOrEditHolidayDto());
            this.modal().show();
        } else {
            this._holidaysService.getHolidayForEdit(holidayId).subscribe((result) => {
                this.holiday.set(result.holiday);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Holiday_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._holidaysService
            .createOrEdit(this.holiday())
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

    updateField(field: keyof CreateOrEditHolidayDto, value: any): void {
        const current = this.holiday();
        (current as any)[field] = value;
        const next = new CreateOrEditHolidayDto();
        Object.assign(next, current);
        this.holiday.set(next);
    }
}
