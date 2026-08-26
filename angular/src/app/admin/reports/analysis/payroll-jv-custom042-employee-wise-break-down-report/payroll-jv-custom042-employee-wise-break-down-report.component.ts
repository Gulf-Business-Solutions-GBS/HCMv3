import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import {
    GetPayrollJVCustom042EmployeeWiseBreakDownReportInput,
    PayrollJVCustom042EmployeeWiseBreakDownReportServiceProxy,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 042) "EmployeeWiseBreakDown JV" analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom042.ascx(.cs),
 * cmbType == "EmployeeWiseBreakDown JV" branch. One of ten live cmbType formats on that legacy screen, each ported as
 * its own report/screen -- see the app service remarks for the full family and the shared Main/Sub/Div/
 * Dept/Anly1/Anly2/Act1/Act2/Currency/Fc-amount/Net-amount/Dr-Cr column shape (fixed, unlike the 019/023
 * family's server-driven dynamic column list, so this table's header/body are static).
 *
 * ATYPICAL FILTER PANEL vs the reference screen: legacy's panel is Month/Year, Run Type, Payroll Area and
 * Employee Code; there is no date range and no employee-status combo. The Payroll Area filter is carried
 * in the input but not exposed in this panel (same shortcut as the Custom 023 Angular screen).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-jv-custom042-employee-wise-break-down-report.component.html',
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
    ],
})
export class PayrollJVCustom042EmployeeWiseBreakDownReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom042EmployeeWiseBreakDownReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);

    readonly employeeCode = signal<string>('');

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly exporting = signal<boolean>(false);

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom042EmployeeWiseBreakDownReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items || []);
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
            .getPayrollJVCustom042EmployeeWiseBreakDownReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollJVCustom042EmployeeWiseBreakDownReportInput {
        return new GetPayrollJVCustom042EmployeeWiseBreakDownReportInput({
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            payrollAreaId: undefined,
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            payrollRunType: this.payrollRunType(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
