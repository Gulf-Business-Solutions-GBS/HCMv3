import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
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
    EmployeeStatus,
    GetAllPayrollAreasInput,
    GetLateEarlyLogWithPhotoReportInput,
    LateEarlyLogWithPhotoReportServiceProxy,
    PayrollAreasServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface LateEarlyLogWithPhotoReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Late/Early Log With Photo analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLogWithPhoto.ascx(.cs).
 *
 * THE PHOTO. The legacy "GeoSelfie" column was a rendered IMAGE: a DevExpress binary-image column
 * fed by reading TimeEvent.GeoSelfiePath off the web server file system. HCMv3 has no equivalent
 * source -- only the GeoSelfiePath STRING was ported, blob storage being deferred -- so the server
 * returns the stored path and this screen shows that path as text. It is flagged, not dropped
 * silently; rendering a real thumbnail needs the deferred file-storage work first.
 *
 * Despite the name this is NOT "late-early-log plus a photo": it is an older, narrower sibling
 * with no OrgLevel columns and no hours columns.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack,
 * the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './late-early-log-with-photo-report.component.html',
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
export class LateEarlyLogWithPhotoReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(LateEarlyLogWithPhotoReportServiceProxy);
    private readonly _payrollAreaService = inject(PayrollAreasServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbPayrollArea. Holds PayrollArea.Code, which is what the legacy combo's value was. */
    readonly payrollAreaCode = signal<string | undefined>(undefined);
    readonly payrollAreaCodeOptions = signal<{ label: string; value: string }[]>([]);

    readonly exporting = signal<boolean>(false);

    /** Legacy grid columns, in legacy column order. */
    readonly columns: LateEarlyLogWithPhotoReportColumn[] = [
        { field: 'geoSelfiePath', header: this.l('GeoSelfie'), sortable: false, width: 260 },
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'date', header: this.l('Date'), sortable: true, width: 130, isDate: true },
        { field: 'shiftStart', header: this.l('ShiftStart'), sortable: false, width: 130 },
        { field: 'shiftEnd', header: this.l('ShiftEnd'), sortable: false, width: 130 },
        { field: 'signIn', header: this.l('SignIn'), sortable: false, width: 120 },
        { field: 'signOut', header: this.l('SignOut'), sortable: false, width: 120 },
        { field: 'earlyIn', header: this.l('EarlyIn'), sortable: false, width: 120 },
        { field: 'earlyOut', header: this.l('EarlyOut'), sortable: false, width: 120 },
        { field: 'lateIn', header: this.l('LateIn'), sortable: false, width: 120 },
        { field: 'lateOut', header: this.l('LateOut'), sortable: false, width: 120 },
        { field: 'punchInLocation', header: this.l('PunchInLocation'), sortable: false, width: 180 },
        { field: 'punchOutLocation', header: this.l('PunchOutLocation'), sortable: false, width: 190 },
        { field: 'status', header: this.l('Status'), sortable: false, width: 130 },
        { field: 'weekDay', header: this.l('WeekDay'), sortable: false, width: 130 },
        { field: 'timePolicyName', header: this.l('TimePolicy'), sortable: false, width: 170 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 170 },
        { field: 'countryName', header: this.l('Country'), sortable: false, width: 150 },
    ];

    /**
     * Loads ONLY the report-specific lookups. The grid itself is still not fetched here -- the table's
     * (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._payrollAreaService
            .getAll(
                new GetAllPayrollAreasInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.payrollAreaCodeOptions.set(
                    result.items.map((item) => ({
                        label: item.name ?? item.code ?? '',
                        value: item.code ?? '',
                    }))
                );
            });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getLateEarlyLogWithPhotoReport(
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

    /**
     * Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page --
     * the server ignores the paging arguments on the export endpoint.
     */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getLateEarlyLogWithPhotoReportToExcel(
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
    ): GetLateEarlyLogWithPhotoReportInput {
        return new GetLateEarlyLogWithPhotoReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
