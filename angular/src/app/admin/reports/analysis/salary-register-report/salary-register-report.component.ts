import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetSalaryRegisterReportInput,
    SalaryRegisterReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/** One grid column: the row field to read and the localization key for its header. */
interface SalaryRegisterColumn {
    field: string;
    header: string;
}

/**
 * Salary Register analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscSalaryRegister.ascx(.cs).
 *
 * ATYPICAL FILTER PANEL vs the reference screen (employee-contract-report): legacy's panel
 * (uscSalaryRegister.ascx:13-21) is dtFrom/dtTo ONLY -- no Run Type, no employee-code box, no
 * employee-status combo -- and GetParameterStr (lines 353-360) logs exactly From Date / To Date. Both
 * shared fields are switched off on <grid-report-filter> rather than by forking it.
 *
 * WIDEST SHEET OF THE WAVE: legacy declares 108 columns (lines 374-495). They are driven from the
 * `columns` list below rather than written out one <th>/<td> at a time, and that list is kept in the
 * SAME ORDER as the server's export column list so the grid and the xlsx always agree.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './salary-register-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class SalaryRegisterReportComponent extends AppComponentBase {
    private readonly _reportService = inject(SalaryRegisterReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (this screen uses the date range only) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());

    readonly exporting = signal<boolean>(false);

    /** The date column is rendered through the luxon pipe; every other column is a plain number. */
    readonly dateField = 'inPeriod';

    /**
     * Legacy column order (uscSalaryRegister.ascx.cs:374-495), mirroring the server's export list.
     */
    readonly columns: SalaryRegisterColumn[] = [
        { field: 'employeeCode', header: 'Code' },
        { field: 'employeeName', header: 'Employee' },
        { field: 'inPeriod', header: 'Inperiod' },
        { field: 'housingAllowance', header: 'HousingAllowance' },
        { field: 'transportAllowance', header: 'TransportAllowance' },
        { field: 'airTicketPay', header: 'AirTicketPay' },
        { field: 'hajjLeavePay', header: 'HajjLeavePay' },
        { field: 'loanPay', header: 'LoanPay' },
        { field: 'oTonNormalDay', header: 'OTonNormalDay' },
        { field: 'oTonWeekend', header: 'OTonWeekend' },
        { field: 'oTonPublicHoliday', header: 'OTonPublicHoliday' },
        { field: 'gratuityPay', header: 'GratuityPay' },
        { field: 'loanRecoveryDeduction', header: 'LoanRecoveryDeduction' },
        { field: 'sickLeaveUnpaidDed', header: 'SickLeaveUnpaidDed' },
        { field: 'ticketProvision', header: 'TicketProvision' },
        { field: 'annualLeavePaidDays', header: 'AnnualLeavePaidDays' },
        { field: 'absentUnpaidDays', header: 'AbsentUnpaidDays' },
        { field: 'sickLeavePaidDays', header: 'SickLeavePaidDays' },
        { field: 'sickLeaveUnpaidDays', header: 'SickLeaveUnpaidDays' },
        { field: 'gratuityDays', header: 'GratuityDays' },
        { field: 'gratuityPaidDays', header: 'GratuityPaidDays' },
        { field: 'gratuityBalanceDays', header: 'GratuityBalanceDays' },
        { field: 'publicHolidayDays', header: 'PublicHolidayDays' },
        { field: 'weeklyOffDays', header: 'WeeklyOffDays' },
        { field: 'restDays', header: 'RestDays' },
        { field: 'advancedLeavePaidDays', header: 'AdvancedLeavePaidDays' },
        { field: 'advancedTicketPaidDays', header: 'AdvancedTicketPaidDays' },
        { field: 'mobileAllowance', header: 'MobileAllowance' },
        { field: 'commission', header: 'Commission' },
        { field: 'paidDays', header: 'PaidDays' },
        { field: 'medicalInsuranceDed', header: 'MedicalInsuranceDed' },
        { field: 'advancePaymentDed', header: 'AdvancePaymentDed' },
        { field: 'cashShortageDed', header: 'CashShortageDed' },
        { field: 'sickLeavePay', header: 'SickLeavePay' },
        { field: 'extraWorkingDayPay', header: 'ExtraWorkingDayPay' },
        { field: 'lieuDayPay', header: 'LieuDayPay' },
        { field: 'extraWorkingPaidDays', header: 'ExtraWorkingPaidDays' },
        { field: 'lieuPaidDays', header: 'LieuPaidDays' },
        { field: 'advanceLeavePay', header: 'AdvanceLeavePay' },
        { field: 'advanceLeaveRecoveryDays', header: 'AdvanceLeaveRecoveryDays' },
        { field: 'advanceLeavePaidRecovery', header: 'AdvanceLeavePaidRecovery' },
        { field: 'miscellaneousDed', header: 'MiscellaneousDed' },
        { field: 'inactiveDays', header: 'InactiveDays' },
        { field: 'totalActiveDays', header: 'TotalActiveDays' },
        { field: 'totalAbsentUnpaidDays', header: 'TotalAbsentUnpaidDays' },
        { field: 'totalGratuityDays', header: 'TotalGratuityDays' },
        { field: 'totalGratuityPaidDays', header: 'TotalGratuityPaidDays' },
        { field: 'employeeTicketCurrentMonthAccrual', header: 'EmployeeTicketCurrentMonthAccrual' },
        { field: 'totalEmployeeTicketAccrual', header: 'TotalEmployeeTicketAccrual' },
        { field: 'basicSalaryArrear', header: 'BasicSalaryArrear' },
        { field: 'housingAllowanceArrear', header: 'HousingAllowanceArrear' },
        { field: 'transportAllowanceArrear', header: 'TransportAllowanceArrear' },
        { field: 'annualLeavePayArrear', header: 'AnnualLeavePayArrear' },
        { field: 'airTicketPayArrear', header: 'AirTicketPayArrear' },
        { field: 'hajjLeavePayArrear', header: 'HajjLeavePayArrear' },
        { field: 'loanPayArrear', header: 'LoanPayArrear' },
        { field: 'oTonNormalDayArr', header: 'OTonNormalDayArr' },
        { field: 'oTonWeekendArr', header: 'OTonWeekendArr' },
        { field: 'oTonPublicHolidayArr', header: 'OTonPublicHolidayArr' },
        { field: 'gratuityPayArr', header: 'GratuityPayArr' },
        { field: 'lateEarlyDeductionArrear', header: 'LateEarlyDeductionArrear' },
        { field: 'loanRecoveryDeductionArrear', header: 'LoanRecoveryDeductionArrear' },
        { field: 'pIFSSContributionDedArr', header: 'PIFSSContributionDedArr' },
        { field: 'sickLeaveUnpaidDedArr', header: 'SickLeaveUnpaidDedArr' },
        { field: 'gratuityProvisionArr', header: 'GratuityProvisionArr' },
        { field: 'annualLeaveProvisionArr', header: 'AnnualLeaveProvisionArr' },
        { field: 'ticketProvisionArr', header: 'TicketProvisionArr' },
        { field: 'annualLeavePaidDaysArr', header: 'AnnualLeavePaidDaysArr' },
        { field: 'absentUnpaidDaysArr', header: 'AbsentUnpaidDaysArr' },
        { field: 'sickLeavePaidDaysArr', header: 'SickLeavePaidDaysArr' },
        { field: 'sickLeaveUnpaidDaysArr', header: 'SickLeaveUnpaidDaysArr' },
        { field: 'gratuityDaysArr', header: 'GratuityDaysArr' },
        { field: 'gratuityPaidDaysArr', header: 'GratuityPaidDaysArr' },
        { field: 'gratuityBalanceDaysArr', header: 'GratuityBalanceDaysArr' },
        { field: 'publicHolidayDaysArr', header: 'PublicHolidayDaysArr' },
        { field: 'weeklyOffDaysArr', header: 'WeeklyOffDaysArr' },
        { field: 'restDaysArr', header: 'RestDaysArr' },
        { field: 'annualLeaveCurrentMonthAccrualArrear', header: 'AnnualLeaveCurrentMonthAccrualArrear' },
        { field: 'activeDaysinaMonthArrear', header: 'ActiveDaysinaMonthArrear' },
        { field: 'advancedLeavePaidDaysArrear', header: 'AdvancedLeavePaidDaysArrear' },
        { field: 'advancedTicketPaidDaysArrear', header: 'AdvancedTicketPaidDaysArrear' },
        { field: 'mobileAllowanceArrear', header: 'MobileAllowanceArrear' },
        { field: 'commissionArrear', header: 'CommissionArrear' },
        { field: 'presentDaysArrear', header: 'PresentDaysArrear' },
        { field: 'paidDaysArrear', header: 'PaidDaysArrear' },
        { field: 'medicalInsuranceDedArr', header: 'MedicalInsuranceDedArr' },
        { field: 'advancePaymentDedArr', header: 'AdvancePaymentDedArr' },
        { field: 'cashShortageDedArr', header: 'CashShortageDedArr' },
        { field: 'absentUnpaidDedArr', header: 'AbsentUnpaidDedArr' },
        { field: 'sickLeavePayArrear', header: 'SickLeavePayArrear' },
        { field: 'extraWorkingDayPayArrear', header: 'ExtraWorkingDayPayArrear' },
        { field: 'lieuDayPayArrear', header: 'LieuDayPayArrear' },
        { field: 'extraWorkingPaidDaysArrear', header: 'ExtraWorkingPaidDaysArrear' },
        { field: 'lieuPaidDaysArrear', header: 'LieuPaidDaysArrear' },
        { field: 'advanceLeavePayArrear', header: 'AdvanceLeavePayArrear' },
        { field: 'advanceLeaveRecoveryDaysArrear', header: 'AdvanceLeaveRecoveryDaysArrear' },
        { field: 'advanceLeavePaidRecoveryArrear', header: 'AdvanceLeavePaidRecoveryArrear' },
        { field: 'miscellaneousPayArrear', header: 'MiscellaneousPayArrear' },
        { field: 'miscellaneousDedArrear', header: 'MiscellaneousDedArrear' },
        { field: 'inactiveDaysArrear', header: 'InactiveDaysArrear' },
        { field: 'annualLeaveOpeningBalArrear', header: 'AnnualLeaveOpeningBalArrear' },
        { field: 'annualLeaveClosingBalArrear', header: 'AnnualLeaveClosingBalArrear' },
        { field: 'totalActiveDaysArrear', header: 'TotalActiveDaysArrear' },
        { field: 'totalAbsentUnpaidDaysArrear', header: 'TotalAbsentUnpaidDaysArrear' },
        { field: 'totalGratuityDaysArrear', header: 'TotalGratuityDaysArrear' },
        { field: 'totalGratuityPaidDaysArrear', header: 'TotalGratuityPaidDaysArrear' },
        { field: 'employeeTicketCurrentMonthAccrualArrear', header: 'EmployeeTicketCurrentMonthAccrualArrear' },
        { field: 'totalEmployeeTicketAccrualArrear', header: 'TotalEmployeeTicketAccrualArrear' },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getSalaryRegisterReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /**
     * Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page.
     */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getSalaryRegisterReportToExcel(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
                    0
                )
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetSalaryRegisterReportInput {
        return new GetSalaryRegisterReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so
            // they must still be listed even though this screen has no such filter.
            employeeCode: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
