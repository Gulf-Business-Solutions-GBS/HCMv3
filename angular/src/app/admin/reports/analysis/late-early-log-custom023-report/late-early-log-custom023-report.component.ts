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
    GetLateEarlyLogCustom023ReportInput,
    LateEarlyLogCustom023ReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Late/Early Log (Custom 023) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom023.ascx(.cs).
 *
 * COLUMN SET: the base Late/Early Log's, MINUS the three break columns (Break Out, Break In, Break
 * Hours). Custom023 has no break machinery at all -- it does not select break punches and never nets a
 * break out of Planned or Total Hours -- so those columns are absent rather than blank.
 *
 * The numbers here are NOT the base report's numbers even where the column names match: early/late deltas
 * are computed from TIMES OF DAY rather than timestamps, Planned Hours come from the raw shift times with
 * a night-shift inversion, punches are suppressed entirely on a week off, and a public holiday blanks
 * every hour column. See the server-side LateEarlyLogCustom023ReportAppService remarks for the detail.
 *
 * The Status column has a richer ladder than the base report: "Leave", "No Return Date", "Week Off",
 * "Rest Day", "Public Holiday", "*", "Absent", or blank on a clean day.
 *
 * FILTER PANEL: dtFrom / dtTo / cmbPayrollArea / txtEmpCode. There is NO employee-status combo -- the
 * legacy query hard-codes Status == Active -- so the shared <grid-report-filter> hides that field rather
 * than being forked, and the payroll-area box is projected into it through <ng-content>.
 *
 * The seven OrgLevel columns are rendered because the legacy grid renders seven -- the standing decision
 * for this wave.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row and the PDF export button.
 */
@Component({
    templateUrl: './late-early-log-custom023-report.component.html',
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
export class LateEarlyLogCustom023ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(LateEarlyLogCustom023ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbPayrollArea, whose value is the PayrollArea CODE. */
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    /**
     * Renders a duration / time-of-day cell as HH:mm.
     *
     * The backend types these as TimeSpan, which NSwag emits as a plain string ("08:00:00"), so there is
     * no luxon DateTime to hand to luxonFormat. Trimming the seconds is done defensively -- a value that
     * is already "08:00", or null, passes through unchanged rather than being sliced blindly.
     */
    formatDuration(value: string | undefined): string {
        if (!value) {
            return '';
        }

        const parts = value.split(':');
        return parts.length >= 2 ? `${parts[0]}:${parts[1]}` : value;
    }

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getLateEarlyLogCustom023Report(
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

    /** Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getLateEarlyLogCustom023ReportToExcel(
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
    ): GetLateEarlyLogCustom023ReportInput {
        return new GetLateEarlyLogCustom023ReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            // Present-but-undefined on purpose: NSwag types this as `EmployeeStatus | undefined`, not as
            // optional, so it must still be listed even though this screen has no status combo and the
            // server ignores the field.
            employeeStatus: undefined,
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
