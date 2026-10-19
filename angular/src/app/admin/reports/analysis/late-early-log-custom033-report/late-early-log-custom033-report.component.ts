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
    GetLateEarlyLogCustom033ReportInput,
    LateEarlyLogCustom033ReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * Late/Early Log (Custom 033) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom033.ascx(.cs).
 *
 * THE LEANEST REPORT OF THE FAMILY. The legacy control comments out Early In, Early Out, Late In, Late
 * Out, Planned Hours, Overtime Hours, Absence Hours, LeaveType, PartialLeaveHours, Time Policy, the two
 * punch-location columns and a never-populated GeoSelfie column, leaving a grid that answers only "when
 * did this person arrive and leave, and for how long". None of the dropped columns is rendered here,
 * because the backend cannot produce a value for any of them.
 *
 * Its near-twin uscRptLateEarlyLog_Custom036 is this same report minus the two access-unit columns; the
 * two share one server-side implementation and differ only in their column list.
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
    templateUrl: './late-early-log-custom033-report.component.html',
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
export class LateEarlyLogCustom033ReportComponent extends AppComponentBase {
    /** OrgLevel1..7 headers use the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));

    private readonly _reportService = inject(LateEarlyLogCustom033ReportServiceProxy);
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
            .getLateEarlyLogCustom033Report(
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
            .getLateEarlyLogCustom033ReportToExcel(
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
    ): GetLateEarlyLogCustom033ReportInput {
        return new GetLateEarlyLogCustom033ReportInput({
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
