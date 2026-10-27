import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    TimeSettingsEditDto,
    ManageTimeSettingsServiceProxy,
    ForfeitRule,
    RosterPlannerFrequency,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditManageTimeSettingsModal',
    templateUrl: './create-or-edit-manage-time-settings-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditManageTimeSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageTimeSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<TimeSettingsEditDto>(new TimeSettingsEditDto());

    readonly forfeitRuleOptions = [
        { value: ForfeitRule.None, name: 'None' },
        { value: ForfeitRule.Monthly, name: 'Monthly' },
        { value: ForfeitRule.Yearly, name: 'Yearly' },
    ];

    readonly rosterPlannerFrequencyOptions = [
        { value: RosterPlannerFrequency.Weekly, name: 'Weekly' },
        { value: RosterPlannerFrequency.Monthly, name: 'Monthly' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new TimeSettingsEditDto();
            record.futureEvaluationInDays = 0;
            record.enableRosterDuty = false;
            record.excludePaidLeaveAccrualOnAbsent = false;
            record.enableTimeAttendanceIntegration = false;
            record.enableMiddleTierTimeEvents = false;
            record.enableAttendanceOnHoliday = false;
            record.enableAttendanceOnWeekOff = false;
            record.enableAttendanceOnRestDay = false;
            record.hourlyLeavelogicEnable = false;
            record.hourlyLeaveShouldSignIn = false;
            record.hourlyLeaveShouldSignOut = false;
            record.enableTimeEngineAutorun = false;
            record.leaveDeductionStartMonth = 0;
            record.enableOvertimeInTimeEngine = false;
            record.timeEventImportPath = undefined;
            record.countWeekoffOnPH = false;
            record.restrictUnpaidleaveIfAnnualleaveAvailable = false;
            record.enableIsTravelling = false;
            record.allowPaidleaveBackdateEntry = false;
            record.sickLeaveAttachMandatory = false;
            record.enableMySqlIntegration = false;
            record.enablePayFullWithPayroll = false;
            record.enableAutoReturnInConsecutiveLeave = false;
            record.countRestDayOnPH = false;
            record.enableTimeAttIntegrationAPI = false;
            record.forfeitRule = ForfeitRule.None;
            record.rosterPlannerFrequency = RosterPlannerFrequency.Weekly;
            record.allowFutureDatedReturnFromLeave = false;
            record.enableAutoHalfDayLeave = false;
            record.enableSettleFractionalLeaveBalance = false;
            record.enableTimeRegisterApproval = false;
            record.autoApproveTimeLog = false;
            record.timeEventSyncBatchCount = 0;
            record.timeEventSyncFreq = 0;
            record.midTierSyncBatchCount = 0;
            record.midTierSyncFreq = 0;
            record.enableBookingQuotaInLeaveRequest = false;
            record.allowDuplicateInTimeEvent = false;
            record.useActualTransInPlannedTimePolicy = false;
            record.absenceViolationRuleId = undefined;
            record.enableGeoFence = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getTimeSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimeSettings_EarliestEvaluationDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditTimeSettings(this.record())
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

    updateField(field: keyof TimeSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new TimeSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof TimeSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    updateNullableNumberField(field: keyof TimeSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : undefined);
    }

    updateDateField(field: keyof TimeSettingsEditDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
