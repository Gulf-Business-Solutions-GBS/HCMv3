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
    GetProjectCostingCustom052ReportInput,
    ProjectCostingCustom052ReportServiceProxy,
    ProjectWageTypeAmountDto,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Project Costing (Custom052) analysis grid report -- per-customer variant of ProjectCostingReportComponent.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectCosting_Custom052.ascx(.cs).
 * Filters by Cost Center instead of Payroll Area; adds WorkingDays/TotalPay/Designation/CostCenter columns
 * -- see ProjectCostingCustom052ReportRowDto remarks on the backend.
 */
@Component({
    templateUrl: './project-costing-custom052-report.component.html',
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
export class ProjectCostingCustom052ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(ProjectCostingCustom052ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly month = signal<number>(new Date().getMonth() + 1);
    readonly year = signal<number>(new Date().getFullYear());
    readonly costCenterCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly components = signal<ProjectWageTypeAmountDto[]>([]);

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getProjectCostingCustom052Report(
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
                this.components.set(result.items.length ? (result.items[0].wageTypeAmounts ?? []) : []);
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
            .getProjectCostingCustom052ReportToExcel(
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
    ): GetProjectCostingCustom052ReportInput {
        return new GetProjectCostingCustom052ReportInput({
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            costCenterCode: this.costCenterCode(),
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
