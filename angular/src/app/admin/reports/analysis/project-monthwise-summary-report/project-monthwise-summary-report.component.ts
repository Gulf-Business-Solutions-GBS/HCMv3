import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import {
    GetProjectMonthwiseSummaryReportInput,
    ProjectMonthwiseAmountDto,
    ProjectMonthwiseSummaryReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * Project Monthwise Summary analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectMonthwiseSummary.ascx(.cs).
 * ATYPICAL: a per-project (WorkingDays, OTHours, Amount) pivot, reorganized project-then-metric instead of
 * legacy's metric-then-project column blocks -- see ProjectMonthwiseSummaryReportRowDto remarks on the
 * backend. Built at runtime from the `projectAmounts` block every row carries.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the WebChart hooks, and the PDF export button.
 */
@Component({
    templateUrl: './project-monthwise-summary-report.component.html',
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
    ],
})
export class ProjectMonthwiseSummaryReportComponent extends AppComponentBase {
    /** OrgLevel1..7 headers use the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));

    private readonly _reportService = inject(ProjectMonthwiseSummaryReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly month = signal<number>(new Date().getMonth() + 1);
    readonly year = signal<number>(new Date().getFullYear());
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly projects = signal<ProjectMonthwiseAmountDto[]>([]);

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getProjectMonthwiseSummaryReport(
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
                this.projects.set(result.items.length ? (result.items[0].projectAmounts ?? []) : []);
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
            .getProjectMonthwiseSummaryReportToExcel(
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
    ): GetProjectMonthwiseSummaryReportInput {
        return new GetProjectMonthwiseSummaryReportInput({
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            payrollAreaCode: this.payrollAreaCode(),
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
