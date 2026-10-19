import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { InputText } from 'primeng/inputtext';
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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetPerformanceAppraisalManagerEvaluationReportInput,
    PerformanceAppraisalManagerEvaluationReportServiceProxy,
    PerformanceAppraisalManagerEvaluationRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * Performance Appraisal Manager Evaluation analysis grid report.
 *
 * Ported legacy screen (customer variant):
 * HCM10.Web/UserControls/Reports/Analysis/PerformanceReport/uscRptPerformanceAppraisal_Custom035.ascx(.cs).
 * Detail mode's dynamic "KPI1"/"KPI2".. pivot columns are rendered from the row's `kpis` array rather than
 * legacy's dynamically-numbered DataTable columns -- see the row DTO remarks.
 */
@Component({
    templateUrl: './performance-appraisal-manager-evaluation-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        InputText,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class PerformanceAppraisalManagerEvaluationReportComponent extends AppComponentBase {
    /** OrgLevel1..7 headers use the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));

    private readonly _reportService = inject(PerformanceAppraisalManagerEvaluationReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly cycleName = signal<string>('');
    readonly runType = signal<PerformanceAppraisalManagerEvaluationRunType>(
        PerformanceAppraisalManagerEvaluationRunType.Summary
    );

    readonly exporting = signal<boolean>(false);

    readonly runTypeOptions = [
        { label: this.l('Summary'), value: PerformanceAppraisalManagerEvaluationRunType.Summary },
        { label: this.l('Detail'), value: PerformanceAppraisalManagerEvaluationRunType.Detail },
    ];

    readonly runTypeEnum = PerformanceAppraisalManagerEvaluationRunType;

    /** Widest KPI list across the current page, so the Detail-mode pivot columns line up. */
    maxKpiCount(): number {
        const records = this.primengTableHelper.records() as any[];
        if (!records || records.length === 0) {
            return 0;
        }
        return Math.max(...records.map((r) => (r.kpis ? r.kpis.length : 0)));
    }

    kpiIndexes(): number[] {
        return Array.from({ length: this.maxKpiCount() }, (_, i) => i);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPerformanceAppraisalManagerEvaluationReport(
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
            .getPerformanceAppraisalManagerEvaluationReportToExcel(
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
    ): GetPerformanceAppraisalManagerEvaluationReportInput {
        return new GetPerformanceAppraisalManagerEvaluationReportInput({
            employeeCode: this.employeeCode(),
            cycleName: this.cycleName(),
            runType: this.runType(),
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
