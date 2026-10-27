import { Component, inject, signal, viewChild } from '@angular/core';
import { NgClass } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetNotificationSentLogsInput, NotificationRulesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

/**
 * What a rule has sent: one row per record it notified about (or marked as sent), newest first. Opened for
 * one rule from its row, or with no rule to list every HCM notice, the fixed daily ones included.
 */
@Component({
    selector: 'notificationSentLogModal',
    templateUrl: './notification-sent-log-modal.component.html',
    imports: [AppBsModalDirective, NgClass, TableModule, PaginatorModule, LocalizePipe, LuxonFormatPipe],
})
export class NotificationSentLogModalComponent extends AppComponentBase {
    private _rulesService = inject(NotificationRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('sentLogModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly active = signal<boolean>(false);
    readonly title = signal<string>('');

    private notificationMasterId: number | undefined;

    show(notificationMasterId?: number, title?: string): void {
        this.notificationMasterId = notificationMasterId;
        this.title.set(title ?? '');
        this.primengTableHelper.records.set([]);
        this.primengTableHelper.totalRecordsCount.set(0);

        // The table loads itself through (onLazyLoad) once it is rendered by active().
        this.active.set(true);
        this.modal().show();
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._rulesService
            .getSentLogs(
                new GetNotificationSentLogsInput({
                    notificationMasterId: this.notificationMasterId,
                    fromDate: undefined,
                    toDate: undefined,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
