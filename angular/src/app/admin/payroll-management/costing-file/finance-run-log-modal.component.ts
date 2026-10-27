import { Component, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { FinanceRunLogFilter, GetFinanceRunLogInput } from '@shared/service-proxies/service-proxies';
import { FinanceScreenApi, downloadBase64File } from './finance-screen-api';

/**
 * Legacy "Cost Execution Log" popup (uscCostingFile.ascx popupPayrollLog, .ascx.cs lines 2911-2973): every
 * ProjectLog row of the run with All / Successful / Failed filters, the employee, the time, the status and the
 * FULL message -- for a rejected D365 batch that is the request, D365's whole reply and the JSON that was sent --
 * plus Export Log.
 */
@Component({
    selector: 'financeRunLogModal',
    templateUrl: './finance-run-log-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        BusyIfDirective,
        LuxonFormatPipe,
    ],
})
export class FinanceRunLogModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('logModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly FinanceRunLogFilter = FinanceRunLogFilter;

    readonly active = signal<boolean>(false);
    readonly exporting = signal<boolean>(false);
    readonly statusFilter = signal<FinanceRunLogFilter>(FinanceRunLogFilter.All);
    readonly filterText = signal<string>('');
    readonly allCount = signal<number>(0);
    readonly successCount = signal<number>(0);
    readonly failedCount = signal<number>(0);

    private _api: FinanceScreenApi | undefined;

    show(api: FinanceScreenApi, filter: FinanceRunLogFilter = FinanceRunLogFilter.All): void {
        this._api = api;
        this.statusFilter.set(filter);
        this.filterText.set('');
        this.primengTableHelper.records.set([]);
        this.primengTableHelper.totalRecordsCount.set(0);
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    getRows(event?: LazyLoadEvent): void {
        if (!this._api) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const input = this.buildInput();
        input.sorting = this.primengTableHelper.getSorting(this.dataTable());
        input.maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);
        input.skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);

        this._api
            .getProcessLog(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.allCount.set(result.allCount);
                this.successCount.set(result.successCount);
                this.failedCount.set(result.failedCount);
            });
    }

    setStatusFilter(filter: FinanceRunLogFilter): void {
        this.statusFilter.set(filter);
        this.reloadFromFirstPage();
    }

    applySearch(): void {
        this.reloadFromFirstPage();
    }

    /** Legacy btnExportPayrollLog_Click: the grid as filtered, every page. */
    exportLog(): void {
        if (!this._api) {
            return;
        }

        const input = this.buildInput();
        input.sorting = this.primengTableHelper.getSorting(this.dataTable());

        this.exporting.set(true);
        this._api
            .exportProcessLog(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((result) => downloadBase64File(result));
    }

    private reloadFromFirstPage(): void {
        this.getRows({
            first: 0,
            rows: this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
        } as LazyLoadEvent);
    }

    private buildInput(): GetFinanceRunLogInput {
        return new GetFinanceRunLogInput({
            statusFilter: this.statusFilter(),
            filter: this.filterText() ? this.filterText() : undefined,
            sorting: undefined,
            skipCount: 0,
            maxResultCount: 1000,
        });
    }
}
