import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PayrollSettingsEditDto,
    ManagePayrollSettingsServiceProxy,
    OvertimeHoursBasis,
    SocialSecurityRateRule,
    PickOffcyclePayment,
    LeavePaymentType,
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
    selector: 'createOrEditManagePayrollSettingsModal',
    templateUrl: './create-or-edit-manage-payroll-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManagePayrollSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManagePayrollSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<PayrollSettingsEditDto>(new PayrollSettingsEditDto());

    readonly overtimeHoursBasisOptions = [
        { value: OvertimeHoursBasis.FixedHours, name: 'FixedHours' },
        { value: OvertimeHoursBasis.DutyHours, name: 'DutyHours' },
    ];

    readonly socialSecurityRateRuleOptions = [
        { value: SocialSecurityRateRule.WageSalary, name: 'WageSalary' },
        { value: SocialSecurityRateRule.ContractSalary, name: 'ContractSalary' },
    ];

    readonly pickOffcyclePaymentOptions = [
        { value: PickOffcyclePayment.CurrentDate, name: 'CurrentDate' },
        { value: PickOffcyclePayment.CurrentMonth, name: 'CurrentMonth' },
    ];

    readonly leavePaymentTypeOptions = [
        { value: LeavePaymentType.None, name: 'None' },
        { value: LeavePaymentType.PayWithPayroll, name: 'PayWithPayroll' },
        { value: LeavePaymentType.PayInAdvance, name: 'PayInAdvance' },
        { value: LeavePaymentType.PayFullWithPayroll, name: 'PayFullWithPayroll' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new PayrollSettingsEditDto();
            record.clientId = '';
            record.defaultWorkingHours = 0;
            record.payslipSignature = false;
            record.leaveAdvPaymentdateBefore15 = false;
            record.leaveAdvPaymentdateAfter15 = false;
            record.leaveProvisionAsPerCurrentSalary = false;
            record.enableNotReturnFromLeaveLogic = false;
            record.enableBasicpayMoreThenFixedDays = false;
            record.enableUnpaidDedSlabWise = false;
            record.enableFixedDaysLogicChange = false;
            record.generateRegisterOnNotReturnFromLeave = false;
            record.offcycleBankFileCutoff = 0;
            record.roundNetPay = false;
            record.applyAnnualleaveNoImpactLogic = false;
            record.enablePayrollEngineAutorun = false;
            record.decimal6RoundingFrom = undefined as any;
            record.fullMonthLeaveAdjForFixedDays = false;
            record.payLeaveAsPerCutoff = false;
            record.decimalInSalaryRegister = 0;
            record.defaultLeavePaymentType = LeavePaymentType.None;
            record.allowDeleteBackdatedSalaryRegister = false;
            record.autoFillFnFCompensation = false;
            record.editableAutoFillFnFCompensation = false;
            record.fixedToActualWhenLeaveOverlapOnMonthStart = false;
            record.fixedToActualWhenLeaveOverlapOnMonthEnd = false;
            record.fixedToActualWhenUpaidLeaveFound = false;
            record.fixedToActualWhenAbsentDayFound = false;
            record.overtimeHoursBasis = OvertimeHoursBasis.FixedHours;
            record.carryAllowanceOnSalaryImport = false;
            record.socialSecurityRateRule = SocialSecurityRateRule.WageSalary;
            record.allowHealthReimburseBalCarryFwd = false;
            record.allowSimulationRun = false;
            record.runOffCycleForFutureDate = false;
            record.disableRetroCalculation = false;
            record.pickOffcyclePayment = PickOffcyclePayment.CurrentDate;
            record.regularLeaveEncashOnBasicPayOnly = false;
            record.fnFLeaveEncashOnBasicPayOnly = false;
            record.showBillingTypeInPaymentScreen = false;
            record.autoUpdateLoanRecoveryEndDate = false;
            record.changePartialPeriodFactoringInFixedDaysBilling = false;
            record.partialPeriodCutoffDayInFixedDaysBilling = 0;
            record.enableEffectiveDateInRecPayment = false;
            record.bankFileMinAmount = 0;
            record.includeSalaryIncProvDiffInSalReg = false;
            record.includeSalaryIncProvDiffInSalRegEffDt = undefined as any;
            record.lockSalaryRegisterOnPayrollExit = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPayrollSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayrollSettings_ClientId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditPayrollSettings(this.record())
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

    updateField(field: keyof PayrollSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new PayrollSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof PayrollSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    updateDateField(field: keyof PayrollSettingsEditDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
