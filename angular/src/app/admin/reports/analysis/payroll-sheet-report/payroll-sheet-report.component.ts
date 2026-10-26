import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    GetPayrollSheetReportInput,
    PayrollRunType,
    PayrollSheetReportRowDto,
    PayrollSheetReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * One grid column. `localize` distinguishes the fixed columns (whose header is a localization key)
 * from the data-driven wage-type columns (whose header is already-resolved master data and must NOT be
 * passed through the localize pipe -- it would render as "[Basic Salary]").
 */
interface PayrollSheetColumn {
    field: string;
    header: string;
    localize: boolean;
}

/**
 * Payroll Sheet analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet.ascx(.cs),
 * generic branch.
 *
 * DATA-DRIVEN COLUMN SET: the sheet has ~28 fixed columns followed by one column per wage type actually
 * present in the filtered period (plus a " Days" column for leave wage types). The server returns those
 * lists alongside the rows, so the grid and the exported xlsx are driven by ONE server-side column list
 * and can never drift apart.
 *
 * The OrgLevel columns are captioned with the tenant's Organization Unit Type names, and levels the tenant
 * has not defined are hidden (see `injectOrgLevelCaptions`); the exported xlsx still carries all seven.
 *
 * ATYPICAL FILTER PANEL vs the reference screen (employee-contract-report): legacy's panel is
 * Month/Year, To-Month/To-Year, Run Type, Payroll Area, Reason Type and an employee-code box -- there is
 * no date range and no employee-status combo, so those are switched off on <grid-report-filter> rather
 * than by forking it.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-sheet-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class PayrollSheetReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollSheetReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state: this screen keeps the employee-code box only ---
    readonly employeeCode = signal<string>('');

    // --- report-specific filters (legacy txtMonth/txtYear/txtMonthTo/txtYearTo/cmbRunType/...) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly toMonth = signal<number | undefined>(new Date().getMonth() + 1);
    readonly toYear = signal<number | undefined>(new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly payrollReasonTypeId = signal<number | undefined>(undefined);

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly exporting = signal<boolean>(false);

    /** Legacy fixed column order, mirroring the server's export list. */
    private static readonly FIXED_COLUMNS: PayrollSheetColumn[] = [
        { field: 'serialNumber', header: 'SrNo', localize: true },
        { field: 'employeeCode', header: 'Code', localize: true },
        { field: 'employeeName', header: 'Employee', localize: true },
        { field: 'payrollAreaName', header: 'PayrollArea', localize: true },
        { field: 'monthYear', header: 'MonthYear', localize: true },
        { field: 'payrollRunType', header: 'RunType', localize: true },
        { field: 'payrollReasonType', header: 'ReasonType', localize: true },
        { field: 'offCyclePaymentDate', header: 'OffCyclePaymentDate', localize: true },
        { field: 'jobTitle', header: 'JobTitle', localize: true },
        { field: 'orgLevel1', header: 'OrgLevel1', localize: true },
        { field: 'orgLevel2', header: 'OrgLevel2', localize: true },
        { field: 'orgLevel3', header: 'OrgLevel3', localize: true },
        { field: 'orgLevel4', header: 'OrgLevel4', localize: true },
        { field: 'orgLevel5', header: 'OrgLevel5', localize: true },
        { field: 'orgLevel6', header: 'OrgLevel6', localize: true },
        { field: 'orgLevel7', header: 'OrgLevel7', localize: true },
        { field: 'workLocation', header: 'WorkLocation', localize: true },
        { field: 'salaryVia', header: 'SalaryVia', localize: true },
        { field: 'currency', header: 'Currency', localize: true },
        { field: 'mosalId', header: 'MOSALID', localize: true },
        { field: 'nameOfSponsor', header: 'NameOfSponsor', localize: true },
        { field: 'ibanNumber', header: 'IBANNumber', localize: true },
        { field: 'civilId', header: 'CivilId', localize: true },
        { field: 'costCenterCode', header: 'CostCenterCode', localize: true },
        { field: 'costCenterName', header: 'CostCenter', localize: true },
        { field: 'paymentType', header: 'PaymentType', localize: true },
        { field: 'accountNo', header: 'AccountNo', localize: true },
        { field: 'payerBank', header: 'PayerBank', localize: true },
    ];

    readonly columns = signal<PayrollSheetColumn[]>(PayrollSheetReportComponent.FIXED_COLUMNS);

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly displayColumns = this.orgLevels.columns(() => this.columns());

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollSheetReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                const basicPay = result.actualBasicPayColumns || [];
                const earningDeduction = result.earningDeductionColumns || [];
                const leaveDays = result.leaveDaysColumns || [];

                this.columns.set(this.buildColumns(basicPay, earningDeduction, leaveDays));

                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(
                    (result.items || []).map((item) => this.flatten(item, basicPay, earningDeduction, leaveDays))
                );
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
            .getPayrollSheetReportToExcel(
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

    /**
     * Legacy column order: the fixed columns, then the "Act ..." basic-pay columns, then Total Salary and
     * Working Days, then per wage type its " Days" column (leave wage types only, BEFORE the amount --
     * legacy line 2841) and its amount column, and finally Difference.
     */
    private buildColumns(basicPay: string[], earningDeduction: string[], leaveDays: string[]): PayrollSheetColumn[] {
        const columns: PayrollSheetColumn[] = [...PayrollSheetReportComponent.FIXED_COLUMNS];

        for (const name of basicPay) {
            columns.push({ field: 'act::' + name, header: 'Act ' + name, localize: false });
        }

        columns.push({ field: 'totalSalary', header: 'TotalSalary', localize: true });
        columns.push({ field: 'workingDays', header: 'WorkingDays', localize: true });

        for (const name of earningDeduction) {
            if (leaveDays.indexOf(name) !== -1) {
                columns.push({ field: 'days::' + name, header: name + ' Days', localize: false });
            }
            columns.push({ field: 'amt::' + name, header: name, localize: false });
        }

        columns.push({ field: 'difference', header: 'Difference', localize: true });

        return columns;
    }

    /** Flattens the row's dictionaries into the prefixed field names buildColumns declared. */
    private flatten(
        row: PayrollSheetReportRowDto,
        basicPay: string[],
        earningDeduction: string[],
        leaveDays: string[]
    ): Record<string, unknown> {
        const flat: Record<string, unknown> = { ...(row as unknown as Record<string, unknown>) };

        for (const name of basicPay) {
            flat['act::' + name] = row.actualBasicPayAmounts?.[name] ?? 0;
        }

        for (const name of earningDeduction) {
            if (leaveDays.indexOf(name) !== -1) {
                flat['days::' + name] = row.wageDays?.[name] ?? 0;
            }
            flat['amt::' + name] = row.wageAmounts?.[name] ?? 0;
        }

        return flat;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollSheetReportInput {
        return new GetPayrollSheetReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so
            // they must still be listed even though this screen has no date range and no status combo.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            toMonth: this.toMonth(),
            toYear: this.toYear(),
            payrollRunType: this.payrollRunType(),
            payrollReasonTypeId: this.payrollReasonTypeId(),
            payrollAreaId: this.payrollAreaId(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
