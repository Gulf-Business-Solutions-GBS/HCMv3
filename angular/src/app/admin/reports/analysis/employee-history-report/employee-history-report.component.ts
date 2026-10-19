import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
    EmployeeHistoryReportServiceProxy,
    EmployeeStatus,
    GetEmployeeHistoryReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface HistoryReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee History analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeHistory.ascx(.cs).
 *
 * ATYPICAL ROW GRAIN: one row per employee PER CHANGE DATE (the union of basic-pay and org-assignment start
 * dates), not one row per employee.
 *
 * LEGACY QUIRK KEPT: the backend's base query is hard-coded to Status = Active and the status filter is
 * ANDed on top, so choosing anything else returns no rows. The filter is still shown because legacy showed
 * it -- see the app service remarks.
 *
 * Legacy hides both date pickers on this screen (Visible="false" in the .ascx).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-history-report.component.html',
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
export class EmployeeHistoryReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeHistoryReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /** Legacy column order (uscRptEmployeeHistory.ascx.cs:499-521). */
    private readonly allColumns: HistoryReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'employeeNameAr', header: this.l('FullNameAr'), sortable: true, width: 220 },
        { field: 'startDate', header: this.l('StartDate'), sortable: true, width: 140, isDate: true },
        { field: 'jobTitle', header: this.l('JobTitle'), sortable: true, width: 180 },
        { field: 'jobTitleAr', header: this.l('JobTitleAr'), sortable: false, width: 180 },
        { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
        { field: 'employeeGroupName', header: this.l('EmployeeGroup'), sortable: false, width: 160 },
        { field: 'employeeSubGroupName', header: this.l('EmployeeSubGroup'), sortable: false, width: 180 },
        { field: 'basicSalary', header: this.l('BasicSalary'), sortable: false, width: 140 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 160 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 160 },
        { field: 'workflowGroupName', header: this.l('WorkflowGroup'), sortable: false, width: 170 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
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
            .getEmployeeHistoryReport(
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
            .getEmployeeHistoryReportToExcel(
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

    private buildInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetEmployeeHistoryReportInput {
        return new GetEmployeeHistoryReportInput({
            // fromDate/toDate are deliberately not sent: legacy hides both pickers on this screen.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
