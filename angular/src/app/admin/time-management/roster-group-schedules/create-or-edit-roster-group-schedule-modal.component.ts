import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterGroupScheduleDto,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutyPeriodsInput,
    GetAllRosterGroupsInput,
    RosterGroupDto,
    RosterGroupSchedulesServiceProxy,
    RosterGroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditRosterGroupScheduleModal',
    templateUrl: './create-or-edit-roster-group-schedule-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterGroupScheduleModalComponent extends AppComponentBase {
    private _rosterGroupSchedulesService = inject(RosterGroupSchedulesServiceProxy);
    private _rosterGroupsService = inject(RosterGroupsServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterGroupSchedule = signal<CreateOrEditRosterGroupScheduleDto>(new CreateOrEditRosterGroupScheduleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'RosterGroupSchedule',
            record: this.rosterGroupSchedule,
            isNew: this.isNew,
            keys: () => ({
                RosterGroupId: this.rosterGroupSchedule().rosterGroupId,
                DutyPeriodId: this.rosterGroupSchedule().dutyPeriodId,
            }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly rosterGroups = signal<RosterGroupDto[]>([]);
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);

    show(rosterGroupScheduleId?: number): void {
        this.isNew.set(!rosterGroupScheduleId);
        this.active.set(true);

        this._rosterGroupsService
            .getAll(
                new GetAllRosterGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.rosterGroups.set(result.items);
            });

        this._dutyPeriodsService
            .getAll(
                new GetAllDutyPeriodsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.dutyPeriods.set(result.items);
            });

        if (!rosterGroupScheduleId) {
            this.rosterGroupSchedule.set(new CreateOrEditRosterGroupScheduleDto());
            this.modal().show();
        } else {
            this._rosterGroupSchedulesService
                .getRosterGroupScheduleForEdit(rosterGroupScheduleId)
                .subscribe((result) => {
                    this.rosterGroupSchedule.set(result.rosterGroupSchedule);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('RosterGroupSchedule_RosterGroupId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterGroupSchedulesService
            .createOrEdit(this.rosterGroupSchedule())
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

    updateField(field: keyof CreateOrEditRosterGroupScheduleDto, value: any): void {
        const current = this.rosterGroupSchedule();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterGroupScheduleDto();
        Object.assign(next, current);
        this.rosterGroupSchedule.set(next);
    }

    updateDateField(field: keyof CreateOrEditRosterGroupScheduleDto, value: string): void {
        const current = this.rosterGroupSchedule();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditRosterGroupScheduleDto();
        Object.assign(next, current);
        this.rosterGroupSchedule.set(next);
    }
}
