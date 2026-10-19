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
    GetPayrollSheetCustom023ReportInput,
    PayrollRunType,
    PayrollSheetCustom023ReportRowDto,
    PayrollSheetCustom023ReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * One grid column. `localize` distinguishes the fixed columns (whose header is a localization key)
 * from the data-driven wage-type columns (whose header is already-resolved master data and must NOT be
 * passed through the localize pipe -- it would render as "[Basic Salary]").
 */
interface PayrollSheetCustom023Column {
    field: string;
    header: string;
    localize: boolean;
}

/**
 * Payroll Sheet (Custom 023) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet_Custom023.ascx(.cs).
 *
 * FIXED COLUMN SET: unlike its generic sibling, this customer variant hard-codes its columns in the
 * legacy control -- ~28 fixed columns, then seven "Act ..." basic-pay columns, then 56
 * earning/deduction/net columns. The server still returns those lists alongside the rows so the grid and
 * the exported xlsx are driven by ONE server-side column list. There are no leave " Days" columns here,
 * and wage types are matched by NAME rather than by code.
 *
 * The OrgLevel columns are captioned with the tenant's Organization Unit Type names, and levels the tenant
 * has not defined are hidden (see `injectOrgLevelCaptions`); the exported xlsx still carries all seven.
 *
 * ATYPICAL FILTER PANEL vs the reference screen (employee-contract-report): legacy's panel
 * (uscRptPayrollSheet_Custom023.ascx:24-48) is Month/Year, To-Month/To-Year, Run Type, Payroll Area and
 * Reason Type. It has NO employee-code box -- its generic sibling does, at line 68 -- and no date range
 * and no employee-status combo, so all three are switched off on <grid-report-filter> rather than by
 * forking it. The backend ignores the inherited EmployeeCode field to match.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-sheet-custom023-report.component.html',
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
export class PayrollSheetCustom023ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollSheetCustom023ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

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
    private static readonly FIXED_COLUMNS: PayrollSheetCustom023Column[] = [
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

    readonly columns = signal<PayrollSheetCustom023Column[]>(PayrollSheetCustom023ReportComponent.FIXED_COLUMNS);

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
            .getPayrollSheetCustom023Report(
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

                this.columns.set(this.buildColumns(basicPay, earningDeduction));

                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(
                    (result.items || []).map((item) => this.flatten(item, basicPay, earningDeduction))
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
            .getPayrollSheetCustom023ReportToExcel(
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
     * Working Days, then the 56 earning/deduction/net amount columns, and finally Difference.
     */
    private buildColumns(basicPay: string[], earningDeduction: string[]): PayrollSheetCustom023Column[] {
        const columns: PayrollSheetCustom023Column[] = [...PayrollSheetCustom023ReportComponent.FIXED_COLUMNS];

        for (const name of basicPay) {
            columns.push({ field: 'act::' + name, header: 'Act ' + name, localize: false });
        }

        columns.push({ field: 'totalSalary', header: 'TotalSalary', localize: true });
        columns.push({ field: 'workingDays', header: 'WorkingDays', localize: true });

        for (const name of earningDeduction) {
            columns.push({ field: 'amt::' + name, header: name, localize: false });
        }

        columns.push({ field: 'difference', header: 'Difference', localize: true });

        return columns;
    }

    /** Flattens the row's dictionaries into the prefixed field names buildColumns declared. */
    private flatten(
        row: PayrollSheetCustom023ReportRowDto,
        basicPay: string[],
        earningDeduction: string[]
    ): Record<string, unknown> {
        const flat: Record<string, unknown> = { ...(row as unknown as Record<string, unknown>) };

        for (const name of basicPay) {
            flat['act::' + name] = row.actualBasicPayAmounts?.[name] ?? 0;
        }

        for (const name of earningDeduction) {
            flat['amt::' + name] = row.wageAmounts?.[name] ?? 0;
        }

        return flat;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollSheetCustom023ReportInput {
        return new GetPayrollSheetCustom023ReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so
            // they must still be listed even though this screen has no date range and no status combo.
            fromDate: undefined,
            toDate: undefined,
            // Legacy Custom 023 has no employee-code box at all; the server ignores this field too.
            employeeCode: undefined,
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
