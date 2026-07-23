import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BreakShiftsServiceProxy,
    CreateOrEditBreakShiftDto,
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
    selector: 'createOrEditBreakShiftModal',
    templateUrl: './create-or-edit-break-shift-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditBreakShiftModalComponent extends AppComponentBase {
    private _breakShiftsService = inject(BreakShiftsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly breakShift = signal<CreateOrEditBreakShiftDto>(new CreateOrEditBreakShiftDto());

    show(breakShiftId?: number): void {
        this.active.set(true);

        if (!breakShiftId) {
            this.breakShift.set(new CreateOrEditBreakShiftDto());
            this.modal().show();
        } else {
            this._breakShiftsService.getBreakShiftForEdit(breakShiftId).subscribe((result) => {
                this.breakShift.set(result.breakShift);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('BreakShift_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._breakShiftsService
            .createOrEdit(this.breakShift())
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

    updateField(field: keyof CreateOrEditBreakShiftDto, value: any): void {
        const current = this.breakShift();
        (current as any)[field] = value;
        const next = new CreateOrEditBreakShiftDto();
        Object.assign(next, current);
        this.breakShift.set(next);
    }

    updateDateField(field: keyof CreateOrEditBreakShiftDto, value: string): void {
        const current = this.breakShift();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditBreakShiftDto();
        Object.assign(next, current);
        this.breakShift.set(next);
    }
}
