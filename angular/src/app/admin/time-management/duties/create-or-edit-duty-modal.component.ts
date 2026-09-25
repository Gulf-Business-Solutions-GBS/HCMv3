import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BreakShiftDto,
    BreakShiftsServiceProxy,
    CreateOrEditDutyDto,
    DutiesServiceProxy,
    GetAllBreakShiftsInput,
    GetAllOvertimeTypesInput,
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
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditDutyModal',
    templateUrl: './create-or-edit-duty-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDutyModalComponent extends AppComponentBase {
    private _dutiesService = inject(DutiesServiceProxy);
    private _breakShiftsService = inject(BreakShiftsServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly duty = signal<CreateOrEditDutyDto>(new CreateOrEditDutyDto());
    readonly breakShifts = signal<BreakShiftDto[]>([]);
    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    show(dutyId?: number): void {
        this.active.set(true);

        this._breakShiftsService
            .getAll(new GetAllBreakShiftsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.breakShifts.set(result.items);
            });

        this._overtimeTypesService
            .getAll(new GetAllOvertimeTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.overtimeTypes.set(result.items);
            });

        if (!dutyId) {
            this.duty.set(new CreateOrEditDutyDto());
            this.modal().show();
        } else {
            this._dutiesService.getDutyForEdit(dutyId).subscribe((result) => {
                this.duty.set(result.duty);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Duty_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._dutiesService
            .createOrEdit(this.duty())
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

    updateField(field: keyof CreateOrEditDutyDto, value: any): void {
        const current = this.duty();
        (current as any)[field] = value;
        const next = new CreateOrEditDutyDto();
        Object.assign(next, current);
        this.duty.set(next);
    }

    updateDateField(field: keyof CreateOrEditDutyDto, value: string): void {
        const current = this.duty();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditDutyDto();
        Object.assign(next, current);
        this.duty.set(next);
    }
}
