import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BreakShiftDto,
    BreakShiftsServiceProxy,
    CreateOrEditDailyDutyDto,
    DailyDutiesServiceProxy,
    GetAllBreakShiftsInput,
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
    selector: 'createOrEditDailyDutyModal',
    templateUrl: './create-or-edit-daily-duty-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDailyDutyModalComponent extends AppComponentBase {
    private _dailyDutiesService = inject(DailyDutiesServiceProxy);
    private _breakShiftsService = inject(BreakShiftsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly dailyDuty = signal<CreateOrEditDailyDutyDto>(new CreateOrEditDailyDutyDto());
    readonly breakShifts = signal<BreakShiftDto[]>([]);

    show(dailyDutyId?: number): void {
        this.active.set(true);

        this._breakShiftsService
            .getAll(
                new GetAllBreakShiftsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.breakShifts.set(result.items);
            });

        if (!dailyDutyId) {
            this.dailyDuty.set(new CreateOrEditDailyDutyDto());
            this.modal().show();
        } else {
            this._dailyDutiesService.getDailyDutyForEdit(dailyDutyId).subscribe((result) => {
                this.dailyDuty.set(result.dailyDuty);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DailyDuty_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._dailyDutiesService
            .createOrEdit(this.dailyDuty())
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

    updateField(field: keyof CreateOrEditDailyDutyDto, value: any): void {
        const current = this.dailyDuty();
        (current as any)[field] = value;
        const next = new CreateOrEditDailyDutyDto();
        Object.assign(next, current);
        this.dailyDuty.set(next);
    }

    updateDateField(field: keyof CreateOrEditDailyDutyDto, value: string): void {
        const current = this.dailyDuty();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditDailyDutyDto();
        Object.assign(next, current);
        this.dailyDuty.set(next);
    }
}
