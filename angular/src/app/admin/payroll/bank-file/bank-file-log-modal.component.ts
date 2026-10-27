import { Component, inject, signal, viewChild } from '@angular/core';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    BankFileLogDto,
    BankFileLogFilter,
    BankFilesServiceProxy,
    GetBankFileLogsInput,
} from '@shared/service-proxies/service-proxies';

/**
 * Port of legacy uscBankFile's "Bank File Generation Log" popup (uscBankFile.ascx popupPayrollLog,
 * uscBankFile.ascx.cs ASPxCallbackPanelGrid_Callback / SetLogGridDataSource / btnExportTimeLog_Click): every run,
 * newest first, with the All / Successful / Failed filters and their counts, and Export Log. Each run also offers
 * the copy of the file it produced, which legacy stored but never let anyone download.
 */
@Component({
    selector: 'bankFileLogModal',
    templateUrl: './bank-file-log-modal.component.html',
    imports: [
        AppBsModalDirective,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
        BusyIfDirective,
        LuxonFormatPipe,
    ],
})
export class BankFileLogModalComponent extends AppComponentBase {
    private _bankFileService = inject(BankFilesServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);

    readonly modal = viewChild<ModalDirective>('logModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly BankFileLogFilter = BankFileLogFilter;

    readonly active = signal<boolean>(false);
    readonly working = signal<boolean>(false);
    readonly statusFilter = signal<BankFileLogFilter>(BankFileLogFilter.All);

    readonly allCount = signal<number>(0);
    readonly successfulCount = signal<number>(0);
    readonly failedCount = signal<number>(0);

    show(): void {
        this.statusFilter.set(BankFileLogFilter.All);
        this.primengTableHelper.records.set([]);
        this.primengTableHelper.totalRecordsCount.set(0);
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    // The PrimeNG table's (onLazyLoad) fires the first load -- show() does not call this.
    getLogs(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const input = this.buildInput();
        input.sorting = this.primengTableHelper.getSorting(this.dataTable());
        input.maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);
        input.skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);

        this._bankFileService
            .getBankFileLogs(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.allCount.set(result.allCount);
                this.successfulCount.set(result.successfulCount);
                this.failedCount.set(result.failedCount);
            });
    }

    setStatusFilter(filter: BankFileLogFilter): void {
        this.statusFilter.set(filter);
        this.getLogs({
            first: 0,
            rows: this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
        } as LazyLoadEvent);
    }

    /** Legacy btnExportTimeLog_Click: the rows the current filter shows. */
    exportLog(): void {
        const input = this.buildInput();
        input.sorting = this.primengTableHelper.getSorting(this.dataTable());

        this.working.set(true);
        this._bankFileService
            .exportBankFileLogs(input)
            .pipe(finalize(() => this.working.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    download(record: BankFileLogDto): void {
        this.working.set(true);
        this._bankFileService
            .getBankFileLogDownload(record.id)
            .pipe(finalize(() => this.working.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    statusBadgeClass(record: BankFileLogDto): string {
        return record.succeeded ? 'badge badge-light-success' : 'badge badge-light-danger';
    }

    private buildInput(): GetBankFileLogsInput {
        return new GetBankFileLogsInput({
            statusFilter: this.statusFilter(),
            sorting: undefined,
            skipCount: 0,
            maxResultCount: 1000,
        });
    }
}
