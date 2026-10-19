import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
    EmployeeInfoReportServiceProxy,
    EmployeeStatus,
    GetEmployeeInfoReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface InfoReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee Info analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeInfo.ascx(.cs) --
 * with uscRptEmployeeCTC the largest control in the family (~70 fixed columns plus one dynamic column per
 * basic-pay wage type).
 *
 * The column list is built in TypeScript rather than hand-written as ~70 <th> elements, because the wage
 * type columns are data-dependent: the server returns the wage-type name list on every response and the
 * header is rebuilt from it.
 *
 * Legacy hides both date pickers on this screen; every column is "as of today".
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-info-report.component.html',
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
export class EmployeeInfoReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeInfoReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<InfoReportColumn[]>(this.buildColumns([]));

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly displayColumns = this.orgLevels.columns(() => this.columns());

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeInfoReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                const wageTypeNames = result.wageTypeNames || [];
                this.columns.set(this.buildColumns(wageTypeNames));

                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(
                    (result.items || []).map((item) => this.flatten(item, wageTypeNames))
                );
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
            .getEmployeeInfoReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeInfoReportInput {
        return new GetEmployeeInfoReportInput({
            // fromDate/toDate are deliberately not sent: legacy hides both pickers on this screen.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    /** Legacy column order (uscRptEmployeeInfo.ascx.cs:668-763). */
    private buildColumns(wageTypeNames: string[]): InfoReportColumn[] {
        const columns: InfoReportColumn[] = [
            { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
            { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
            { field: 'employeeNameAr', header: this.l('FullNameAr'), sortable: true, width: 220 },
            { field: 'referenceCode', header: this.l('ReferenceCode'), sortable: false, width: 140 },
            { field: 'status', header: this.l('Status'), sortable: true, width: 120 },
            { field: 'employmentType', header: this.l('EmploymentType'), sortable: false, width: 160 },
            { field: 'birthDate', header: this.l('BirthDate'), sortable: true, width: 140, isDate: true },
            { field: 'age', header: this.l('Age'), sortable: false, width: 90 },
            { field: 'bloodGroup', header: this.l('BloodGroup'), sortable: false, width: 120 },
            { field: 'probationDays', header: this.l('ProbationDays'), sortable: false, width: 140 },
            { field: 'joiningDate', header: this.l('JoiningDate'), sortable: true, width: 140, isDate: true },
            {
                field: 'probationEndDate',
                header: this.l('ProbationEndDate'),
                sortable: false,
                width: 160,
                isDate: true,
            },
            { field: 'nationalityName', header: this.l('Nationality'), sortable: false, width: 160 },
            { field: 'religionName', header: this.l('Religion'), sortable: false, width: 140 },
            { field: 'gender', header: this.l('Gender'), sortable: false, width: 120 },
            { field: 'maritalStatusName', header: this.l('MaritalStatus'), sortable: false, width: 150 },
            { field: 'city', header: this.l('City'), sortable: false, width: 140 },
            { field: 'governorateName', header: this.l('Governorate'), sortable: false, width: 150 },
            { field: 'countryName', header: this.l('Country'), sortable: false, width: 140 },
            { field: 'telephone', header: this.l('Telephone'), sortable: false, width: 140 },
            { field: 'mobile', header: this.l('Mobile'), sortable: false, width: 140 },
            { field: 'currentAddress', header: this.l('CurrentAddress'), sortable: false, width: 220 },
            { field: 'permanentAddress', header: this.l('PermanentAddress'), sortable: false, width: 220 },
            { field: 'civilID', header: this.l('CivilID'), sortable: false, width: 150 },
            { field: 'loginID', header: this.l('LoginID'), sortable: false, width: 140 },
            { field: 'accessID', header: this.l('AccessID'), sortable: false, width: 140 },
            { field: 'passportNumber', header: this.l('PassportNumber'), sortable: false, width: 160 },
            { field: 'drivingLicense', header: this.l('DrivingLicense'), sortable: false, width: 160 },
            { field: 'email', header: this.l('Email'), sortable: false, width: 200 },
            { field: 'birthPlaceName', header: this.l('BirthPlace'), sortable: false, width: 150 },
            { field: 'qualificationName', header: this.l('Qualification'), sortable: false, width: 160 },
            { field: 'managerName', header: this.l('Manager'), sortable: false, width: 220 },
            { field: 'jobTitle', header: this.l('JobTitle'), sortable: false, width: 180 },
            { field: 'jobTitleAr', header: this.l('JobTitleAr'), sortable: false, width: 180 },
            { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
            { field: 'employeeGroupName', header: this.l('EmployeeGroup'), sortable: false, width: 160 },
            { field: 'employeeSubGroupName', header: this.l('EmployeeSubGroup'), sortable: false, width: 180 },
            { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 160 },
            { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 160 },
            { field: 'workflowGroupName', header: this.l('WorkflowGroup'), sortable: false, width: 170 },
        ];

        for (let level = 1; level <= 7; level++) {
            columns.push({
                field: 'orgLevel' + level,
                header: this.l('OrgLevel' + level),
                sortable: false,
                width: 160,
            });
            columns.push({
                field: 'orgLevel' + level + 'Ar',
                header: this.l('OrgLevel' + level + 'Ar'),
                sortable: false,
                width: 160,
            });
        }

        columns.push(
            { field: 'sponsorName', header: this.l('Sponsor'), sortable: false, width: 180 },
            { field: 'govtJobTitleName', header: this.l('GovtJobTitle'), sortable: false, width: 180 },
            { field: 'contractSalary', header: this.l('ContractSalary'), sortable: false, width: 160 },
            { field: 'residencyType', header: this.l('ResidencyType'), sortable: false, width: 150 },
            { field: 'workVisaTypeName', header: this.l('WorkVisaType'), sortable: false, width: 160 },
            {
                field: 'civilIdExpiryDate',
                header: this.l('CivilIdExpiryDate'),
                sortable: false,
                width: 170,
                isDate: true,
            }
        );

        wageTypeNames.forEach((name, index) => {
            columns.push({ field: 'wage_' + index, header: name, sortable: false, width: 160 });
        });

        columns.push(
            { field: 'totalAllowance', header: this.l('TotalAllowance'), sortable: false, width: 160 },
            { field: 'totalSalary', header: this.l('TotalSalary'), sortable: false, width: 150 },
            { field: 'currency', header: this.l('Currency'), sortable: false, width: 120 },
            { field: 'separationType', header: this.l('SeparationType'), sortable: false, width: 160 },
            {
                field: 'separationDate',
                header: this.l('SeparationDate'),
                sortable: false,
                width: 150,
                isDate: true,
            },
            {
                field: 'lastDateOfWorking',
                header: this.l('LastDateOfWorking'),
                sortable: false,
                width: 170,
                isDate: true,
            },
            { field: 'fnfStatus', header: this.l('FnfStatus'), sortable: false, width: 150 },
            { field: 'bankName', header: this.l('Bank'), sortable: false, width: 160 },
            { field: 'paymentType', header: this.l('PaymentType'), sortable: false, width: 150 },
            { field: 'accountNum', header: this.l('AccountNum'), sortable: false, width: 170 },
            { field: 'iban', header: this.l('IBAN'), sortable: false, width: 200 },
            { field: 'contractTypeName', header: this.l('ContractType'), sortable: false, width: 160 },
            {
                field: 'contractStartDate',
                header: this.l('ContractStartDate'),
                sortable: false,
                width: 170,
                isDate: true,
            },
            {
                field: 'contractEndDate',
                header: this.l('ContractEndDate'),
                sortable: false,
                width: 170,
                isDate: true,
            },
            { field: 'annualLeaveOpening', header: this.l('AnnualLeaveOpening'), sortable: false, width: 180 },
            { field: 'absentUnpaidOpening', header: this.l('AbsentUnpaidOpening'), sortable: false, width: 180 },
            { field: 'totalLoanBalance', header: this.l('TotalLoanBalance'), sortable: false, width: 170 }
        );

        return columns;
    }

    /** Turns the row's wageAmounts array into flat wage_N fields the generic table can bind to. */
    private flatten(item: any, wageTypeNames: string[]): any {
        const flat: any = { ...item };
        const amounts = item.wageAmounts || [];

        wageTypeNames.forEach((_name, index) => {
            flat['wage_' + index] = amounts[index] ? amounts[index].amount : 0;
        });

        return flat;
    }
}
