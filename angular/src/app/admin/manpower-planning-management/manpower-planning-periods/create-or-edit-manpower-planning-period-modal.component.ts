import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditManpowerPlanningPeriodDto,
    ManpowerPlanningPeriodsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditManpowerPlanningPeriodModal',
    templateUrl: './create-or-edit-manpower-planning-period-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditManpowerPlanningPeriodModalComponent extends AppComponentBase {
    private _service = inject(ManpowerPlanningPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditManpowerPlanningPeriodDto>(new CreateOrEditManpowerPlanningPeriodDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.record.set(new CreateOrEditManpowerPlanningPeriodDto());
            this.modal().show();
        } else {
            this._service.getManpowerPlanningPeriodForEdit(id).subscribe((result) => {
                this.record.set(result.manpowerPlanningPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ManpowerPlanningPeriod_Name')?.focus();
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

    updateField(field: keyof CreateOrEditManpowerPlanningPeriodDto, value: any): void {
        const current = this.record();
        const next = new CreateOrEditManpowerPlanningPeriodDto();
        Object.assign(next, current);
        (next as any)[field] = value;
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditManpowerPlanningPeriodDto, value: string): void {
        const current = this.record();
        const next = new CreateOrEditManpowerPlanningPeriodDto();
        Object.assign(next, current);
        (next as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }
}
