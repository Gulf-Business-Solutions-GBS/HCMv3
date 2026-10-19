import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetProvisionChangeReportInput,
    ProvisionChangeReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface ProvisionChangeReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Provision Change analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptProvisionChange.ascx(.cs).
 *
 * ATYPICAL FILTER PANEL: legacy filters by Month/Year (not a free date range) plus Payroll Area and its own
 * Employee Status combo -- "ALL" (Active + Inactive-with-FnF), "ACTIVE" or "INACTIVE" -- which is a
 * DIFFERENT filter from the shared panel's EmployeeStatus (Active/Inactive only), so the shared status
 * filter is switched off and this one is projected in instead.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './provision-change-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class ProvisionChangeReportComponent extends AppComponentBase {
    private readonly _reportService = inject(ProvisionChangeReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');

    /** Legacy txtMonth/txtYear. */
    readonly month = signal<number | undefined>(undefined);
    readonly year = signal<number | undefined>(undefined);

    /** Legacy cmbPayrollArea selected Code. Blank means "all payroll areas". */
    readonly payrollAreaCode = signal<string>('');

    /** Legacy cmbEmpStatus: "ALL" (default), "ACTIVE" or "INACTIVE". */
    readonly employeeStatusFilter = signal<string | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly employeeStatusFilterOptions = [
        { label: this.l('All'), value: 'ALL' },
        { label: this.l('Active'), value: 'ACTIVE' },
        { label: this.l('Inactive'), value: 'INACTIVE' },
    ];

    private readonly allColumns: ProvisionChangeReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'status', header: this.l('Status'), sortable: false, width: 120 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 170 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 170 },
        { field: 'basicSalary', header: this.l('BasicSalary'), sortable: false, width: 140 },
        { field: 'allowance', header: this.l('Allowance'), sortable: false, width: 130 },
        { field: 'totalSalary', header: this.l('TotalSalary'), sortable: false, width: 140 },
        { field: 'currencyName', header: this.l('Currency'), sortable: false, width: 120 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'totalAlDifferenceAmount', header: this.l('TotalAlDifferenceAmount'), sortable: false, width: 190 },
        {
            field: 'totalIndemnityDifferenceAmount',
            header: this.l('TotalIndemnityDifferenceAmount'),
            sortable: false,
            width: 210,
        },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly columns = this.orgLevels.columns(this.allColumns);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getProvisionChangeReport(
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

    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getProvisionChangeReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetProvisionChangeReportInput {
        return new GetProvisionChangeReportInput({
            // Deliberately not sent: legacy filters by Month/Year, not a free date range.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Deliberately not sent: legacy's own Employee Status combo (ALL/ACTIVE/INACTIVE) replaces the
            // shared panel's Active/Inactive-only filter -- see employeeStatusFilter.
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            payrollAreaCode: this.payrollAreaCode(),
            employeeStatusFilter: this.employeeStatusFilter(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
