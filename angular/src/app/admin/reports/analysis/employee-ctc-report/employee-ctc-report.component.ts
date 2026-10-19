import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
    EmployeeCTCReportServiceProxy,
    EmployeeStatus,
    GetAllPayrollAreasInput,
    GetEmployeeCTCReportInput,
    PayrollAreasServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface CtcReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Employee CTC (cost-to-company) analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeCTC.ascx(.cs) --
 * with uscRptEmployeeInfo the largest control in the family.
 *
 * The column list is built in TypeScript because the basic-pay wage-type columns are data-dependent: the
 * server returns the wage-type name list on every response and the header is rebuilt from it.
 *
 * Legacy hides both date pickers here; the panel is Employee Status, Payroll Area and Employee Code, so the
 * payroll-area dropdown is PROJECTED into the shared filter panel's <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-ctc-report.component.html',
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
export class EmployeeCTCReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(EmployeeCTCReportServiceProxy);
    private readonly _payrollAreaService = inject(PayrollAreasServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);
    readonly payrollAreaId = signal<number | undefined>(undefined);

    readonly payrollAreas = signal<{ label: string; value: number }[]>([]);

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<CtcReportColumn[]>(this.buildColumns([]));

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly displayColumns = this.orgLevels.columns(() => this.columns());

    /**
     * Only the payroll-area lookup is loaded here. The grid itself is NOT fetched in ngOnInit -- the
     * table's (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._payrollAreaService
            .getAll(
                new GetAllPayrollAreasInput({
                    filter: undefined,
                    sorting: 'Code ASC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .subscribe((result) => {
                this.payrollAreas.set(
                    (result.items || []).map((area) => ({
                        label: area.code + ' - ' + area.name,
                        value: area.id,
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
            .getEmployeeCTCReport(
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
            .getEmployeeCTCReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeCTCReportInput {
        return new GetEmployeeCTCReportInput({
            // fromDate/toDate are deliberately not sent: legacy hides both pickers on this screen.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            payrollAreaId: this.payrollAreaId(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    /** Legacy column order (uscRptEmployeeCTC.ascx.cs:1330-1410). */
    private buildColumns(wageTypeNames: string[]): CtcReportColumn[] {
        const columns: CtcReportColumn[] = [
            { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
            { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
            { field: 'employeeNameAr', header: this.l('FullNameAr'), sortable: true, width: 220 },
            { field: 'status', header: this.l('Status'), sortable: true, width: 120 },
            { field: 'birthDate', header: this.l('BirthDate'), sortable: true, width: 140, isDate: true },
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
            { field: 'recruitmentTypeName', header: this.l('RecruitmentType'), sortable: false, width: 160 },
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
            { field: 'email', header: this.l('Email'), sortable: false, width: 200 },
            { field: 'birthPlaceName', header: this.l('BirthPlace'), sortable: false, width: 150 },
            { field: 'residencyType', header: this.l('ResidencyType'), sortable: false, width: 150 },
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
            { field: 'accountNumber', header: this.l('AccountNum'), sortable: false, width: 170 },
            { field: 'iban', header: this.l('IBAN'), sortable: false, width: 200 },
            { field: 'bankBranch', header: this.l('BankBranch'), sortable: false, width: 160 },
            { field: 'sponsorName', header: this.l('Sponsor'), sortable: false, width: 180 },
            { field: 'managerName', header: this.l('Manager'), sortable: false, width: 220 },
            { field: 'jobTitle', header: this.l('JobTitle'), sortable: false, width: 180 },
            { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
            { field: 'employeeGroupName', header: this.l('EmployeeGroup'), sortable: false, width: 160 },
            { field: 'employeeSubGroupName', header: this.l('EmployeeSubGroup'), sortable: false, width: 180 },
        ];

        for (let level = 1; level <= 7; level++) {
            columns.push({
                field: 'orgLevel' + level,
                header: this.l('OrgLevel' + level),
                sortable: false,
                width: 160,
            });
        }

        columns.push(
            { field: 'workLocationCode', header: this.l('WorkLocationCode'), sortable: false, width: 170 },
            { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 160 },
            { field: 'costCenterCode', header: this.l('CostCenterCode'), sortable: false, width: 160 },
            { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 160 },
            { field: 'costCenterGroupName', header: this.l('CostCenterGroup'), sortable: false, width: 180 },
            { field: 'postingGroupName', header: this.l('PostingGroup'), sortable: false, width: 160 },
            { field: 'budgetingGroupName', header: this.l('BudgetingGroup'), sortable: false, width: 170 },
            { field: 'workflowGroupName', header: this.l('WorkflowGroup'), sortable: false, width: 170 },
            {
                field: 'effectiveDate',
                header: this.l('EffectiveDate'),
                sortable: false,
                width: 150,
                isDate: true,
            }
        );

        wageTypeNames.forEach((name, index) => {
            columns.push({ field: 'wage_' + index, header: name, sortable: false, width: 160 });
        });

        columns.push(
            { field: 'totalSalary', header: this.l('TotalSalary'), sortable: false, width: 150 },
            {
                field: 'totalAbsentUnpaidDays',
                header: this.l('TotalAbsentUnpaidDays'),
                sortable: false,
                width: 210,
            },
            { field: 'leaveBalance', header: this.l('LeaveBalance'), sortable: false, width: 160 },
            {
                field: 'leaveProvisionAmount',
                header: this.l('LeaveProvisionAmount'),
                sortable: false,
                width: 200,
            },
            { field: 'indemnityBalance', header: this.l('IndemnityBalance'), sortable: false, width: 180 },
            {
                field: 'indemnityProvisionAmount',
                header: this.l('IndemnityProvisionAmount'),
                sortable: false,
                width: 220,
            },
            { field: 'separationType', header: this.l('SeparationType'), sortable: false, width: 160 },
            {
                field: 'submissionDate',
                header: this.l('SubmissionDate'),
                sortable: false,
                width: 160,
                isDate: true,
            },
            {
                field: 'lastDayInService',
                header: this.l('LastDayInService'),
                sortable: false,
                width: 170,
                isDate: true,
            },
            { field: 'revokeDate', header: this.l('RevokeDate'), sortable: false, width: 150, isDate: true }
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
