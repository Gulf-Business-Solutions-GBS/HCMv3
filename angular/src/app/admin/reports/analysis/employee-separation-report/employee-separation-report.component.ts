import { Component, inject, signal, viewChild } from '@angular/core';
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
    EmployeeSeparationReportServiceProxy,
    GetEmployeeSeparationReportInput,
    SeparationType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface SeparationReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee Separation analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeSeparation.ascx(.cs),
 * filtered on EmployeeSeparation.LastDayInService.
 *
 * DEVIATION: legacy's "Employee Status" box is actually bound to SeparationType -- see
 * GetEmployeeSeparationReportInput remarks. The shared panel's employeeStatus filter is hidden and the
 * correctly-typed SeparationType filter is projected in instead, alongside the Revoked toggle (legacy's
 * session-only Active/Inactive filter, ported explicitly rather than silently dropped).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-separation-report.component.html',
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
export class EmployeeSeparationReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeSeparationReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbEmpStatus (mislabelled -- actually Separation Type). */
    readonly separationType = signal<SeparationType | undefined>(undefined);

    /** Legacy Session[SeparationStatusKey] Active/Inactive toggle. Undefined means "both". */
    readonly isRevoked = signal<boolean | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly separationTypeOptions = [
        { label: this.l('Resignation'), value: SeparationType.Resignation },
        { label: this.l('Termination'), value: SeparationType.Termination },
    ];

    readonly revokedOptions = [
        { label: this.l('Active'), value: false },
        { label: this.l('Revoked'), value: true },
    ];

    readonly columns: SeparationReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'civilId', header: this.l('CivilId'), sortable: false, width: 150 },
        { field: 'salary', header: this.l('Salary'), sortable: false, width: 130 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'separationDate', header: this.l('SeparationDate'), sortable: false, width: 150, isDate: true },
        { field: 'noticePeriodDays', header: this.l('NoticePeriodDays'), sortable: false, width: 160 },
        {
            field: 'lastDayInService',
            header: this.l('ActualLastWorkingDay'),
            sortable: false,
            width: 170,
            isDate: true,
        },
        { field: 'noticePeriodEndQuirk', header: this.l('NoticePeriodEnd'), sortable: false, width: 160, isDate: true },
        {
            field: 'plannedLastDayInService',
            header: this.l('PlannedLastDayInService'),
            sortable: false,
            width: 190,
            isDate: true,
        },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 160 },
        { field: 'separationType', header: this.l('SeparationType'), sortable: false, width: 150 },
        { field: 'separationReasonName', header: this.l('SeparationReason'), sortable: false, width: 180 },
        { field: 'residencyType', header: this.l('ResidencyType'), sortable: false, width: 150 },
        { field: 'residencyExpiry', header: this.l('ResidencyExpiry'), sortable: false, width: 150, isDate: true },
        { field: 'phBalance', header: this.l('PHBalance'), sortable: false, width: 130 },
        { field: 'payStatus', header: this.l('PayStatus'), sortable: false, width: 130 },
        { field: 'countryName', header: this.l('Country'), sortable: false, width: 140 },
        { field: 'revokeDate', header: this.l('RevokeDate'), sortable: false, width: 140, isDate: true },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 160 },
        { field: 'costCenterGroupName', header: this.l('CostCenterGroup'), sortable: false, width: 180 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeSeparationReport(
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

    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getEmployeeSeparationReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeSeparationReportInput {
        return new GetEmployeeSeparationReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            separationType: this.separationType(),
            isRevoked: this.isRevoked(),
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
