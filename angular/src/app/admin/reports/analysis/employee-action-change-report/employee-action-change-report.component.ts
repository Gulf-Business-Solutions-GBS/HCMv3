import { Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
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
    EmployeeActionChangeReportServiceProxy,
    GetAllPersonalActionsInput,
    GetEmployeeActionChangeReportInput,
    PersonalActionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Employee Action Change analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeActionChange.ascx(.cs).
 *
 * ATYPICAL: the legacy panel has ONLY dtFrom/dtTo/cmbPersonalAction, so both the employee-code box and
 * the employee-status dropdown are switched off on the shared filter. The legacy grid also VARIES ITS
 * COLUMN SET with the selected action: OriginalValue/AdjustedValue/FinalValue are not added at all for
 * action codes 7, 8, 1, 2, 5 and 6 (uscRptEmployeeActionChange.ascx.cs:498-508). That is mirrored here
 * by `showValueColumns`; the backend always returns the three fields (they are simply empty on those
 * branches) so the Excel export shape does not depend on the filter.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row and the PDF export button.
 */
@Component({
    templateUrl: './employee-action-change-report.component.html',
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
export class EmployeeActionChangeReportComponent extends AppComponentBase implements OnInit {
    /** Legacy line 500: these action codes render WITHOUT the three value columns. */
    private static readonly ACTION_CODES_WITHOUT_VALUE_COLUMNS = ['7', '8', '1', '2', '5', '6'];

    private readonly _reportService = inject(EmployeeActionChangeReportServiceProxy);
    private readonly _personalActionService = inject(PersonalActionsServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());

    /** Legacy cmbPersonalAction. Holds PersonalAction.Code. */
    readonly personalActionCode = signal<string | undefined>(undefined);
    readonly personalActionOptions = signal<{ label: string; value: string }[]>([]);

    /** Column set applied to the LOADED data, so the grid does not change shape before a re-fetch. */
    readonly loadedActionCode = signal<string | undefined>(undefined);

    readonly showValueColumns = computed(
        () =>
            !EmployeeActionChangeReportComponent.ACTION_CODES_WITHOUT_VALUE_COLUMNS.includes(
                this.loadedActionCode() ?? ''
            )
    );

    readonly exporting = signal<boolean>(false);

    /**
     * Loads ONLY the report-specific lookup. The grid itself is still not fetched here -- the table's
     * (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._personalActionService
            .getAll(new GetAllPersonalActionsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.personalActionOptions.set(
                    result.items.map((item) => ({ label: item.nameEn ?? item.code ?? '', value: item.code ?? '' }))
                );
            });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const requestedActionCode = this.personalActionCode();

        this._reportService
            .getEmployeeActionChangeReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.loadedActionCode.set(requestedActionCode);
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
            .getEmployeeActionChangeReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeActionChangeReportInput {
        return new GetEmployeeActionChangeReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            // Legacy has neither an employee-code nor an employee-status filter on this screen.
            employeeCode: undefined,
            employeeStatus: undefined,
            personalActionCode: this.personalActionCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
