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
    EmployeeStatus,
    EmployeeResidencyReportServiceProxy,
    GetEmployeeResidencyReportInput,
    ResidencyType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface ResidencyReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee Residency analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeResidency.ascx(.cs).
 * ONE ROW PER CIVIL-ID DOCUMENT, not per employee -- see GetEmployeeResidencyReportInput remarks.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-residency-report.component.html',
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
export class EmployeeResidencyReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeResidencyReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbResidencyType. */
    readonly residencyType = signal<ResidencyType | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly residencyTypeOptions = [
        { label: this.l('Company'), value: ResidencyType.Company },
        { label: this.l('NonCompany'), value: ResidencyType.NonCompany },
    ];

    readonly columns: ResidencyReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'civilId', header: this.l('CivilId'), sortable: false, width: 150 },
        { field: 'civilIdIssueDate', header: this.l('CivilIdIssueDate'), sortable: true, width: 160, isDate: true },
        { field: 'civilIdExpiryDate', header: this.l('CivilIdExpiryDate'), sortable: false, width: 160, isDate: true },
        { field: 'passportNo', header: this.l('PassportNo'), sortable: false, width: 150 },
        { field: 'passportIssueDate', header: this.l('PassportIssueDate'), sortable: false, width: 160, isDate: true },
        {
            field: 'passportExpiryDate',
            header: this.l('PassportExpiryDate'),
            sortable: false,
            width: 160,
            isDate: true,
        },
        {
            field: 'residenceExtensionIssueDate',
            header: this.l('ResidenceExtensionIssueDate'),
            sortable: false,
            width: 190,
            isDate: true,
        },
        {
            field: 'residenceExtensionExpiryDate',
            header: this.l('ResidenceExtensionExpiryDate'),
            sortable: false,
            width: 190,
            isDate: true,
        },
        {
            field: 'renewedResidenceInfoIssueDate',
            header: this.l('RenewedResidenceInfoIssueDate'),
            sortable: false,
            width: 200,
            isDate: true,
        },
        {
            field: 'renewedResidenceInfoExpiryDate',
            header: this.l('RenewedResidenceInfoExpiryDate'),
            sortable: false,
            width: 200,
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
        { field: 'residencyType', header: this.l('ResidencyType'), sortable: false, width: 150 },
        { field: 'salaryInIznamal', header: this.l('SalaryInIznamal'), sortable: false, width: 160 },
        { field: 'govtJobTitleName', header: this.l('GovtJobTitle'), sortable: false, width: 180 },
        { field: 'govtJobTitleNameAr', header: this.l('GovtJobTitleAr'), sortable: false, width: 180 },
        { field: 'startDate', header: this.l('StartDate'), sortable: false, width: 140, isDate: true },
        { field: 'endDate', header: this.l('EndDate'), sortable: false, width: 140, isDate: true },
        { field: 'countryName', header: this.l('Country'), sortable: false, width: 140 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeResidencyReport(
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
            .getEmployeeResidencyReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeResidencyReportInput {
        return new GetEmployeeResidencyReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            residencyType: this.residencyType(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
