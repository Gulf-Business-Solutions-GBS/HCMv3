import { Component, computed, inject, signal, viewChild } from '@angular/core';
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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeBasicPayReportRowDto,
    EmployeeBasicPayReportServiceProxy,
    EmployeeStatus,
    GetEmployeeBasicPayReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Employee Basic Pay analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeBasicPay.ascx(.cs).
 *
 * ATYPICAL on three counts -- do not copy them into a plain report:
 *  1. NO DATE FILTER. Legacy renders dtFrom/dtTo but Table1() never reads them (the date predicate is
 *     commented out at lines 552-554) and GetParameterStr() does not log them. The shared filter's date
 *     range is therefore switched off rather than shown as controls that do nothing.
 *  2. DYNAMIC COLUMNS. Legacy generated one grid column per wage type found in the data, ordered by
 *     WageType.PayrollOrder. The backend returns them per row in `wageAmounts`; `wageTypeColumns`
 *     rebuilds the union across the loaded page and the template renders those columns.
 *  3. TWO LEVELS. Legacy had a drill-down grid (Table2(), lines 912-988) listing the wage lines of the
 *     selected pay record. Those lines are already on the level-1 row, so this is an EXPANDABLE ROW
 *     rather than a second round trip -- no drill-up/drill-down session stack is needed.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up session stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row and the PDF export button.
 */
@Component({
    templateUrl: './employee-basic-pay-report.component.html',
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
export class EmployeeBasicPayReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeBasicPayReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /** The legacy dynamic wage-type columns, rebuilt from the loaded rows and ordered by PayrollOrder. */
    readonly wageTypeColumns = computed(() => {
        const rows = this.primengTableHelper.records() as EmployeeBasicPayReportRowDto[];
        const byId = new Map<number, { wageTypeId: number; wageTypeName: string; payrollOrder: number }>();

        for (const row of rows ?? []) {
            for (const wage of row.wageAmounts ?? []) {
                if (!byId.has(wage.wageTypeId)) {
                    byId.set(wage.wageTypeId, {
                        wageTypeId: wage.wageTypeId,
                        wageTypeName: wage.wageTypeName ?? '',
                        payrollOrder: wage.payrollOrder,
                    });
                }
            }
        }

        return [...byId.values()].sort((a, b) => a.payrollOrder - b.payrollOrder);
    });

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    amountOf(record: EmployeeBasicPayReportRowDto, wageTypeId: number): number | undefined {
        return record.wageAmounts?.find((w) => w.wageTypeId === wageTypeId)?.amount;
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeBasicPayReport(
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
            .getEmployeeBasicPayReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeBasicPayReportInput {
        return new GetEmployeeBasicPayReportInput({
            // Legacy ignores the date pickers on this screen -- see the class remarks.
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
