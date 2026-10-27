import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetLateEarlyLogCustom019ReportInput,
    LateEarlyLogCustom019ReportServiceProxy,
    LateEarlyLogCustom019Variant,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * Late/Early Log (Custom 019) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom019.ascx(.cs).
 *
 * THE ONLY LATE/EARLY LOG WITH A VARIANT COMBO. Legacy's cmbVarient offers Detail, Summary and
 * "Summary (Late In > 3 Hours)". The two summary modes change the GRAIN of the report -- one row per
 * (employee, shift) with the four early/late durations and Total Hours summed -- and with it the column
 * set, so this grid switches between two header/body templates rather than merely hiding columns.
 *
 * ATYPICAL FILTER PANEL: where the base control and the other variants filter on cmbPayrollArea,
 * Custom019 replaces it with THREE org-hierarchy combos -- cmbDepartment / cmbDivision / cmbSection,
 * matching legacy OrgLevel2, OrgLevel3 and OrgLevel4 CODES -- plus the variant combo. All four are
 * projected into the shared <grid-report-filter> through <ng-content> rather than forking it. There is NO
 * employee-status combo, since the legacy query hard-codes Status == Active.
 *
 * DETAIL COLUMN SET: Code, Employee, OrgLevel2..OrgLevel7, Date, WeekDay, Shift Start, Shift End, Sign
 * In, Sign Out, Early In, Early Out, Late In, Late Out, Total Hours, Planned Hours, Overtime Hours,
 * Absence Hours, Status, LeaveType, PartialLeaveHours, Time Policy, WorkLocation. Legacy comments out
 * AccessId, OrgLevel1, the three break columns, the two access-unit columns, Country and the two
 * punch-location columns, so none of them is rendered.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row and the PDF export button.
 */
@Component({
    templateUrl: './late-early-log-custom019-report.component.html',
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
        LuxonFormatPipe,
    ],
})
export class LateEarlyLogCustom019ReportComponent extends AppComponentBase {
    /** OrgLevel1..7 headers use the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));

    private readonly _reportService = inject(LateEarlyLogCustom019ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbDepartment / cmbDivision / cmbSection -- OrgLevel2 / OrgLevel3 / OrgLevel4 CODES. */
    readonly departmentCode = signal<string>('');
    readonly divisionCode = signal<string>('');
    readonly sectionCode = signal<string>('');

    /** Legacy cmbVarient, whose SelectedIndex = 0 default is Detail. */
    readonly variant = signal<LateEarlyLogCustom019Variant>(LateEarlyLogCustom019Variant.Detail);

    /**
     * Drives the template switch. Bound to the LAST LOADED variant rather than to the combo, so the grid
     * does not swap its columns out from under the rows currently on screen while the user is still
     * choosing; it flips when the new result set arrives.
     */
    readonly loadedVariant = signal<LateEarlyLogCustom019Variant>(LateEarlyLogCustom019Variant.Detail);

    readonly isDetail = computed(() => this.loadedVariant() === LateEarlyLogCustom019Variant.Detail);

    readonly variantOptions = [
        { label: this.l('Detail'), value: LateEarlyLogCustom019Variant.Detail },
        { label: this.l('ReportVariantSummary'), value: LateEarlyLogCustom019Variant.Summary },
        {
            label: this.l('ReportVariantSummaryLateInOverThreeHours'),
            value: LateEarlyLogCustom019Variant.SummaryLateInOverThreeHours,
        },
    ];

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

        const requestedVariant = this.variant();

        this._reportService
            .getLateEarlyLogCustom019Report(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.loadedVariant.set(requestedVariant);
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
            .getLateEarlyLogCustom019ReportToExcel(
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
    ): GetLateEarlyLogCustom019ReportInput {
        return new GetLateEarlyLogCustom019ReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            // Present-but-undefined on purpose: NSwag types this as `EmployeeStatus | undefined`, not as
            // optional, so it must still be listed even though this screen has no status combo and the
            // server ignores the field.
            employeeStatus: undefined,
            departmentCode: this.departmentCode(),
            divisionCode: this.divisionCode(),
            sectionCode: this.sectionCode(),
            variant: this.variant(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
