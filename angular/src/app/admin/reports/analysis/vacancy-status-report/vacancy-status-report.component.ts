import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    AppStatus,
    GetVacancyStatusReportInput,
    VacancyStatusCountDto,
    VacancyStatusReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Vacancy Status analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptVacancyStatus.ascx(.cs).
 * FIXED pivot: one column per AppStatus enum value (unlike the true data-driven pivots elsewhere in this
 * wave, AppStatus is a compile-time-fixed enum), built from the `statusCounts` block every row carries.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, and the commented-out Table2 applicant drill (dead code in legacy).
 */
@Component({
    templateUrl: './vacancy-status-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class VacancyStatusReportComponent extends AppComponentBase {
    private readonly _reportService = inject(VacancyStatusReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly vacancyId = signal<number | undefined>(undefined);
    readonly vacancyStatus = signal<AppStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /** Dynamic per-AppStatus column list -- identical, and identically ordered, on every row. */
    readonly statusColumns = signal<VacancyStatusCountDto[]>([]);

    readonly statusOptions = [
        { label: 'Applied', value: AppStatus.Applied },
        { label: 'Shortlisted', value: AppStatus.Shortlisted },
        { label: 'Interviewed', value: AppStatus.Interviewed },
        { label: 'JobOffer', value: AppStatus.JobOffer },
        { label: 'Hired', value: AppStatus.Hired },
        { label: 'Rejected', value: AppStatus.Rejected },
        { label: 'Joined', value: AppStatus.Joined },
    ];

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getVacancyStatusReport(
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
                this.statusColumns.set(result.items.length ? (result.items[0].statusCounts ?? []) : []);
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
            .getVacancyStatusReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetVacancyStatusReportInput {
        return new GetVacancyStatusReportInput({
            vacancyId: this.vacancyId(),
            vacancyStatus: this.vacancyStatus(),
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
