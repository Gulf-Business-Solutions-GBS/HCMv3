import { Component, inject, input, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BillingType,
    GetAllSalaryRegisterDetailsInput,
    SalaryRegisterDetailDto,
    SalaryRegisterDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';

/** Technical wage type code of Net Pay (backend PayrollEngineModels NetPay). */
const NET_PAY_WAGE_TYPE_CODE = '9000';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * The wage type lines of one salary register, read-only, in payslip order (the backend's default sort).
 * Legacy SalaryRegister_SalaryRegisterDetail_ListView columns: Wage Type, Rate, Number, Amount.
 */
@Component({
    selector: 'salaryRegisterLines',
    templateUrl: './salary-register-lines.component.html',
    styleUrl: './salary-register-lines.component.css',
    imports: [BusyIfDirective, TableModule, PaginatorModule, DecimalPipe, LocalizePipe],
})
export class SalaryRegisterLinesComponent extends AppComponentBase {
    private _detailsService = inject(SalaryRegisterDetailsServiceProxy);

    readonly salaryRegisterId = input.required<number>();

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /**
     * Legacy CustomizeAppearanceController row colours: Net Pay slate grey; earnings and deductions that
     * impact net pay green and salmon; everything else (time types, provisions, non-net-pay) plain.
     */
    rowClass(record: SalaryRegisterDetailDto): string {
        if (record.wageTypeCode === NET_PAY_WAGE_TYPE_CODE) {
            return 'sr-line-net-pay';
        }
        if (record.dontImpactNetPay) {
            return 'sr-line-plain';
        }
        if (record.billingType === BillingType.Earning) {
            return 'sr-line-earning';
        }
        if (record.billingType === BillingType.Deduction) {
            return 'sr-line-deduction';
        }
        return 'sr-line-plain';
    }

    // No getRecords() in ngOnInit: the PrimeNG table's (onLazyLoad) triggers the first load.

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._detailsService
            .getAll(
                new GetAllSalaryRegisterDetailsInput({
                    salaryRegisterId: this.salaryRegisterId(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }
}
