import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
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
    EmployeeIncentiveReportServiceProxy,
    EmployeeStatus,
    GetEmployeeIncentiveDetailReportInput,
    GetEmployeeIncentiveReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface IncentiveReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee Incentive analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeIncentive.ascx(.cs).
 *
 * ATYPICAL: unlike the reference screen (employee-contract-report), this report has a real TWO-LEVEL
 * DRILL-DOWN. Legacy's master grid (Table1) is one row per department with a transaction count; clicking a
 * row pushed the department name onto a session stack and rendered Table2, the transaction detail, with a
 * drill-up button. Here the master table's row click swaps in the detail table (with its own paging, its
 * own export and a Back button) and passes the selected org unit ID down; the same filter panel drives
 * both levels.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the session-based drill stack itself, the ColumnChooser
 * JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-incentive-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class EmployeeIncentiveReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeIncentiveReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailTable = viewChild<Table>('detailTable');
    readonly detailPaginator = viewChild<Paginator>('detailPaginator');

    // --- shared filter state (this report DOES use the date range -- legacy shows both pickers) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    // --- drill-down state (legacy: Session[indexKey] + selectedColumnValue) ---
    readonly selectedOrganizationUnitId = signal<number | undefined>(undefined);
    readonly selectedOrganizationUnitName = signal<string>('');

    readonly detailRecords = signal<any[]>([]);
    readonly detailTotalCount = signal<number>(0);
    readonly detailLoading = signal<boolean>(false);

    /** Legacy Table1 columns: Department, Employee (a count). */
    readonly masterColumns: IncentiveReportColumn[] = [
        { field: 'organizationUnitName', header: this.l('OrganizationUnit'), sortable: true, width: 320 },
        { field: 'employeeCount', header: this.l('Employee'), sortable: true, width: 160 },
    ];

    /** Legacy Table2 columns (uscRptEmployeeIncentive.ascx.cs:558-576). */
    private readonly allDetailColumns: IncentiveReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitle', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'costCenterCode', header: this.l('CostCenterCode'), sortable: false, width: 160 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 160 },
        { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
        { field: 'wageTypeName', header: this.l('WageType'), sortable: false, width: 180 },
        { field: 'paymentDate', header: this.l('PaymentDate'), sortable: true, width: 140, isDate: true },
        { field: 'paymentNumber', header: this.l('PaymentNumber'), sortable: true, width: 160 },
        { field: 'paymentUnit', header: this.l('PaymentUnit'), sortable: false, width: 140 },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly detailColumns = this.orgLevels.columns(this.allDetailColumns);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeIncentiveReport(
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
        // Re-running the filters always returns to the master level, exactly like legacy's Get Data button.
        this.selectedOrganizationUnitId.set(undefined);

        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /** Legacy drill-down: clicking a master row rendered Table2 for that department. */
    drillDown(record: any): void {
        if (record.organizationUnitId == null) {
            return;
        }

        this.selectedOrganizationUnitId.set(record.organizationUnitId);
        this.selectedOrganizationUnitName.set(record.organizationUnitName);
        this.detailRecords.set([]);
        this.detailTotalCount.set(0);
    }

    /** Legacy drill-up button. */
    drillUp(): void {
        this.selectedOrganizationUnitId.set(undefined);
        this.detailRecords.set([]);
        this.detailTotalCount.set(0);
    }

    getDetailRecords(event?: LazyLoadEvent): void {
        if (this.selectedOrganizationUnitId() == null) {
            return;
        }

        this.detailLoading.set(true);

        this._reportService
            .getEmployeeIncentiveDetailReport(
                this.buildDetailInput(
                    this.primengTableHelper.getSorting(this.detailTable()),
                    this.primengTableHelper.getMaxResultCount(this.detailPaginator(), event),
                    this.primengTableHelper.getSkipCount(this.detailPaginator(), event)
                )
            )
            .pipe(finalize(() => this.detailLoading.set(false)))
            .subscribe((result) => {
                this.detailTotalCount.set(result.totalCount);
                this.detailRecords.set(result.items);
            });
    }

    /** Exports the MASTER level -- every matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getEmployeeIncentiveReportToExcel(
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

    /** Exports the DETAIL level -- every matching row for the drilled-into org unit. */
    exportDetailToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getEmployeeIncentiveDetailReportToExcel(
                this.buildDetailInput(
                    this.primengTableHelper.getSorting(this.detailTable()),
                    this.detailPaginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
                    0
                )
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    private buildInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetEmployeeIncentiveReportInput {
        return new GetEmployeeIncentiveReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    private buildDetailInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetEmployeeIncentiveDetailReportInput {
        return new GetEmployeeIncentiveDetailReportInput({
            organizationUnitId: this.selectedOrganizationUnitId(),
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
