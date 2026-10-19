import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
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
    EmployeeActionReportServiceProxy,
    GetAllPersonalActionsInput,
    GetEmployeeActionReportInput,
    PersonalActionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

/**
 * Employee Action analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeAction.ascx(.cs).
 *
 * ATYPICAL: the legacy panel has NO cmbEmpStatus, so the shared filter's employee-status dropdown is
 * switched off with [showEmployeeStatus]="false" rather than by forking the component. The
 * report-specific cmbPersonalAction is projected into the panel's <ng-content /> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row and the PDF export button.
 */
@Component({
    templateUrl: './employee-action-report.component.html',
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
export class EmployeeActionReportComponent extends AppComponentBase implements OnInit {
    /** OrgLevel1..7 headers use the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));

    private readonly _reportService = inject(EmployeeActionReportServiceProxy);
    private readonly _personalActionService = inject(PersonalActionsServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbPersonalAction. Holds PersonalAction.Code, which is what the legacy combo's value was. */
    readonly personalActionCode = signal<string | undefined>(undefined);
    readonly personalActionOptions = signal<{ label: string; value: string }[]>([]);

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

        this._reportService
            .getEmployeeActionReport(
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
            .getEmployeeActionReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeActionReportInput {
        return new GetEmployeeActionReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            // Legacy has no employee-status filter on this screen; the field stays unset on purpose.
            employeeStatus: undefined,
            personalActionCode: this.personalActionCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
