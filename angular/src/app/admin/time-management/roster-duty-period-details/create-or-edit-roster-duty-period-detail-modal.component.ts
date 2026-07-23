import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterDutyPeriodDetailDto,
    DailyDutiesServiceProxy,
    DailyDutyDto,
    GetAllDailyDutiesInput,
    GetAllRosterDutyPeriodsInput,
    RosterDutyPeriodDetailsServiceProxy,
    RosterDutyPeriodDto,
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
    selector: 'createOrEditRosterDutyPeriodDetailModal',
    templateUrl: './create-or-edit-roster-duty-period-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterDutyPeriodDetailModalComponent extends AppComponentBase {
    private _rosterDutyPeriodDetailsService = inject(RosterDutyPeriodDetailsServiceProxy);
    private _rosterDutyPeriodsService = inject(RosterDutyPeriodsServiceProxy);
    private _dailyDutiesService = inject(DailyDutiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterDutyPeriodDetail = signal<CreateOrEditRosterDutyPeriodDetailDto>(new CreateOrEditRosterDutyPeriodDetailDto());
    readonly rosterDutyPeriods = signal<RosterDutyPeriodDto[]>([]);
    readonly dailyDuties = signal<DailyDutyDto[]>([]);

    show(rosterDutyPeriodDetailId?: number): void {
        this.active.set(true);

        this._rosterDutyPeriodsService
            .getAll(new GetAllRosterDutyPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.rosterDutyPeriods.set(result.items);
            });

        this._dailyDutiesService
            .getAll(new GetAllDailyDutiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.dailyDuties.set(result.items);
            });

        if (!rosterDutyPeriodDetailId) {
            this.rosterDutyPeriodDetail.set(new CreateOrEditRosterDutyPeriodDetailDto());
            this.modal().show();
        } else {
            this._rosterDutyPeriodDetailsService.getRosterDutyPeriodDetailForEdit(rosterDutyPeriodDetailId).subscribe((result) => {
                this.rosterDutyPeriodDetail.set(result.rosterDutyPeriodDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterDutyPeriodDetail_RosterDutyPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterDutyPeriodDetailsService
            .createOrEdit(this.rosterDutyPeriodDetail())
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

    updateField(field: keyof CreateOrEditRosterDutyPeriodDetailDto, value: any): void {
        const current = this.rosterDutyPeriodDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterDutyPeriodDetailDto();
        Object.assign(next, current);
        this.rosterDutyPeriodDetail.set(next);
    }
}
