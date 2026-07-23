import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterDutyPeriodDto,
    RosterDutyPeriodsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRosterDutyPeriodModal',
    templateUrl: './create-or-edit-roster-duty-period-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterDutyPeriodModalComponent extends AppComponentBase {
    private _rosterDutyPeriodsService = inject(RosterDutyPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterDutyPeriod = signal<CreateOrEditRosterDutyPeriodDto>(new CreateOrEditRosterDutyPeriodDto());

    show(rosterDutyPeriodId?: number): void {
        this.active.set(true);

        if (!rosterDutyPeriodId) {
            this.rosterDutyPeriod.set(new CreateOrEditRosterDutyPeriodDto());
            this.modal().show();
        } else {
            this._rosterDutyPeriodsService.getRosterDutyPeriodForEdit(rosterDutyPeriodId).subscribe((result) => {
                this.rosterDutyPeriod.set(result.rosterDutyPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterDutyPeriod_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterDutyPeriodsService
            .createOrEdit(this.rosterDutyPeriod())
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

    updateField(field: keyof CreateOrEditRosterDutyPeriodDto, value: any): void {
        const current = this.rosterDutyPeriod();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterDutyPeriodDto();
        Object.assign(next, current);
        this.rosterDutyPeriod.set(next);
    }
}
