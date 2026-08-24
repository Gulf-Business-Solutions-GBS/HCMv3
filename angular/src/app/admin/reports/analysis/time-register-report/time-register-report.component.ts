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
    EmployeeStatus,
    GetTimeRegisterReportInput,
    TimeRegisterReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface TimeRegisterReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Time Register analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeRegister.ascx(.cs).
 *
 * Not to be confused with time-register-approval-report, which is a different (narrower) legacy
 * screen showing raw vs approved deductions side by side.
 *
 * The clock columns (ShiftStart, ActualSignIn, LateIn, ...) are preformatted STRINGS from the
 * server, exactly as legacy formatted them, so they are rendered as text.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack,
 * the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './time-register-report.component.html',
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
export class TimeRegisterReportComponent extends AppComponentBase {
    private readonly _reportService = inject(TimeRegisterReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /** Legacy grid columns, in legacy column order. */
    readonly columns: TimeRegisterReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Name'), sortable: true, width: 220 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'dayDate', header: this.l('Date'), sortable: true, width: 130, isDate: true },
        { field: 'shiftStart', header: this.l('ShiftStart'), sortable: false, width: 130 },
        { field: 'actualSignIn', header: this.l('ActualSignIn'), sortable: false, width: 150 },
        { field: 'shiftEnd', header: this.l('ShiftEnd'), sortable: false, width: 130 },
        { field: 'actualSignOut', header: this.l('ActualSignOut'), sortable: false, width: 160 },
        { field: 'totalWorkingHours', header: this.l('TotalWorkingHours'), sortable: false, width: 180 },
        { field: 'lateIn', header: this.l('LateIn'), sortable: false, width: 120 },
        { field: 'deductionLateIn', header: this.l('DeductionLateIn'), sortable: false, width: 170 },
        { field: 'earlyOut', header: this.l('EarlyOut'), sortable: false, width: 120 },
        { field: 'deductionEarlyOut', header: this.l('DeductionEarlyOut'), sortable: false, width: 190 },
        {
            field: 'deductionAfterMonthlyTolerance',
            header: this.l('DeductionAfterMonthlyTolerance'),
            sortable: false,
            width: 270,
        },
        { field: 'totalLateMinutes', header: this.l('TotalLateMinutes'), sortable: false, width: 180 },
        { field: 'absentDay', header: this.l('AbsentDay'), sortable: false, width: 130 },
        { field: 'countryName', header: this.l('Country'), sortable: false, width: 150 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 170 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getTimeRegisterReport(
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
            .getTimeRegisterReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetTimeRegisterReportInput {
        return new GetTimeRegisterReportInput({
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
