import { Component, OnInit, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    PayslipInput,
    PayslipReportLanguage,
    PayrollRunType,
    ReportsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * Ported legacy screen: HCM10.Web/UserControls/PayrollManagement/uscPreviewPaySlip.ascx(.cs).
 *
 * PREVIEW = one employee, one period, rendered on screen (the legacy code opened
 * `InstantPreviewPaySlip.aspx` in a new browser tab). It never mails anything. The bulk/mailing screen is
 * `payslip-report` (legacy uscRptPaySlip).
 *
 * Legacy parameter parity: employee, month, year, payroll area, payroll run type, employee status,
 * report type (language). Payroll area is intentionally NOT offered here: the legacy SQL short-circuits it
 * whenever an employee code is supplied, so it never affected the previewed document.
 */
@Component({
    templateUrl: './report-preview.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class ReportPreviewComponent extends AppComponentBase implements OnInit {
    private _employeesService = inject(EmployeesServiceProxy);
    private _reportsService = inject(ReportsServiceProxy);

    readonly employees = signal<EmployeeDto[]>([]);
    readonly selectedEmployeeId = signal<number | undefined>(undefined);
    readonly generatingSalaryCertificate = signal<boolean>(false);
    readonly generatingPayslip = signal<boolean>(false);

    // Legacy uscPreviewPaySlip defaulted the report to the PREVIOUS month (rptPayslipEn ctor:
    // DateTime.Now.AddMonths(-1)).
    readonly month = signal<number>(new Date().getMonth() === 0 ? 12 : new Date().getMonth());
    readonly year = signal<number>(new Date().getMonth() === 0 ? new Date().getFullYear() - 1 : new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);
    readonly language = signal<PayslipReportLanguage>(PayslipReportLanguage.English);

    // Legacy filled the year combo from distinct PayrollCalendar.PayrollYear values. There is no
    // payroll-calendar lookup endpoint on this screen's service surface, so a rolling window is used
    // instead (deviation, reported).
    readonly years: number[] = Array.from({ length: 8 }, (_, i) => new Date().getFullYear() - 5 + i);
    readonly months: number[] = Array.from({ length: 12 }, (_, i) => i + 1);

    ngOnInit(): void {
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    previewSalaryCertificate(): void {
        const employeeId = this.selectedEmployeeId();
        if (!employeeId) {
            this.message.warn(this.l('PleaseSelectAnEmployeeFirst'));
            return;
        }

        this.generatingSalaryCertificate.set(true);
        const pdfTab = window.open('', '_blank');
        this._reportsService
            .getSalaryCertificatePdf(employeeId)
            .pipe(finalize(() => this.generatingSalaryCertificate.set(false)))
            .subscribe((result) => this.openPdf(pdfTab, result.base64Content));
    }

    previewPayslip(): void {
        const employeeId = this.selectedEmployeeId();
        if (!employeeId) {
            this.message.warn(this.l('PleaseSelectAnEmployeeFirst'));
            return;
        }

        this.generatingPayslip.set(true);
        const pdfTab = window.open('', '_blank');
        this._reportsService
            .getPayslipPdf(
                new PayslipInput({
                    employeeId: employeeId,
                    month: this.month(),
                    year: this.year(),
                    payrollRunType: this.payrollRunType(),
                    employeeStatus: undefined,
                    payrollAreaId: undefined,
                    language: this.language(),
                })
            )
            .pipe(finalize(() => this.generatingPayslip.set(false)))
            .subscribe((result) => this.openPdf(pdfTab, result.base64Content));
    }

    private openPdf(pdfTab: Window | null, base64Content: string): void {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        if (pdfTab) {
            pdfTab.location.href = url;
        } else {
            this.message.warn(this.l('PopupBlockedPleaseAllowPopups'));
        }
    }
}
