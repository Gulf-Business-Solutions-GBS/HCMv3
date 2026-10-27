import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    ApplicantSearchReportServiceProxy,
    Gender,
    GetApplicantSearchReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Applicant Search analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptApplicantSearch.ascx(.cs).
 * Legacy's filter panel is entirely applicant-specific (no employee-code/status/date-range controls), so
 * the shared filter panel's common fields are hidden.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, and the commented-out Table2 (dead code in legacy).
 */
@Component({
    templateUrl: './applicant-search-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class ApplicantSearchReportComponent extends AppComponentBase {
    private readonly _reportService = inject(ApplicantSearchReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly nationalityId = signal<number | undefined>(undefined);
    readonly maritalStatusId = signal<number | undefined>(undefined);
    readonly gender = signal<Gender | undefined>(undefined);
    readonly ageFrom = signal<number | undefined>(undefined);
    readonly ageTo = signal<number | undefined>(undefined);
    readonly firstName = signal<string>('');
    readonly lastName = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly genderOptions = [
        { label: this.l('Male'), value: Gender.Male },
        { label: this.l('Female'), value: Gender.Female },
    ];

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getApplicantSearchReport(
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
            .getApplicantSearchReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetApplicantSearchReportInput {
        return new GetApplicantSearchReportInput({
            nationalityId: this.nationalityId(),
            maritalStatusId: this.maritalStatusId(),
            gender: this.gender(),
            ageFrom: this.ageFrom(),
            ageTo: this.ageTo(),
            firstName: this.firstName(),
            lastName: this.lastName(),
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
