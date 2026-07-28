import { Component, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeStatus,
    GetPayslipRecipientsInput,
    PayrollRunType,
    PayslipRecipientDto,
    PayslipReportLanguage,
    ReportsServiceProxy,
    SendPayslipEmailsInput,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { TableModule } from 'primeng/table';
import { DecimalPipe } from '@angular/common';

/**
 * Ported legacy screen: HCM10.Web/UserControls/PayrollManagement/uscRptPaySlip.ascx(.cs).
 *
 * REPORT = the bulk run. Legacy resolved every employee in the selected payroll area / period that had a
 * non-empty e-mail address and the selected status, rendered each payslip, and MAILED it as a PDF
 * attachment (uscRptPaySlip.Send_PaySlip); the operator only ever saw a success count. Nothing was shown on
 * screen. This port adds an explicit "load recipients" step first, because the legacy screen mailed money
 * documents with no preview of who would receive them.
 *
 * NOT PORTED: the payroll-area narrowing (legacy `B.PayrollArea = '...'`). EmployeeOrgAssignment.PayrollArea
 * is deferred in HCMv3, so the backend refuses the filter outright rather than silently widening the run.
 */
@Component({
    templateUrl: './payslip-report.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, TableModule, DecimalPipe],
})
export class PayslipReportComponent extends AppComponentBase {
    private _reportsService = inject(ReportsServiceProxy);

    readonly month = signal<number>(new Date().getMonth() === 0 ? 12 : new Date().getMonth());
    readonly year = signal<number>(new Date().getMonth() === 0 ? new Date().getFullYear() - 1 : new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);
    readonly language = signal<PayslipReportLanguage>(PayslipReportLanguage.English);
    // Legacy default on the bulk screen was "Active" (the SQL hard-coded a status comparison).
    readonly employeeStatus = signal<EmployeeStatus | undefined>(EmployeeStatus.Active);

    readonly recipients = signal<PayslipRecipientDto[]>([]);
    readonly loading = signal<boolean>(false);
    readonly sending = signal<boolean>(false);

    readonly years: number[] = Array.from({ length: 8 }, (_, i) => new Date().getFullYear() - 5 + i);
    readonly months: number[] = Array.from({ length: 12 }, (_, i) => i + 1);

    loadRecipients(): void {
        this.loading.set(true);
        this._reportsService
            .getPayslipRecipients(
                new GetPayslipRecipientsInput({
                    month: this.month(),
                    year: this.year(),
                    payrollRunType: this.payrollRunType(),
                    employeeStatus: this.employeeStatus(),
                    payrollAreaId: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.recipients.set(result);
                if (!result || result.length === 0) {
                    this.message.info(this.l('NoPayslipRecipientsFound'));
                }
            });
    }

    sendEmails(): void {
        const count = this.recipients().length;
        if (count === 0) {
            this.message.warn(this.l('NoPayslipRecipientsFound'));
            return;
        }

        this.message.confirm(this.l('ConfirmSendPayslipEmails', count), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.sending.set(true);
            this._reportsService
                .sendPayslipEmails(
                    new SendPayslipEmailsInput({
                        month: this.month(),
                        year: this.year(),
                        payrollRunType: this.payrollRunType(),
                        employeeStatus: this.employeeStatus(),
                        payrollAreaId: undefined,
                        employeeId: undefined,
                        language: this.language(),
                    })
                )
                .pipe(finalize(() => this.sending.set(false)))
                .subscribe((result) => {
                    this.notify.info(
                        this.l('PayslipEmailsSentSummary', result.successCount, result.failureCount, result.totalCount)
                    );
                });
        });
    }
}
