import {
    ChangeDetectionStrategy,
    Component,
    ViewEncapsulation,
    inject,
    viewChild,
    signal,
    AfterViewInit,
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationServiceProxy,
    UserNotification,
    UserNotificationState,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { IFormattedUserNotification, UserNotificationHelper } from './UserNotificationHelper';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../common/sub-header/sub-header.component';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDateRangePickerLuxonModifierDirective } from '../../../../shared/utils/date-time/primeng-date-range-picker-luxon-modifier.directive';
import { FormsModule } from '@angular/forms';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';

import { Tooltip } from 'primeng/tooltip';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ViewNotificationModalComponent } from './view-notification-modal.component';

@Component({
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        SubHeaderComponent,
        DatePickerModule,
        PrimengDateRangePickerLuxonModifierDirective,
        FormsModule,
        BusyIfDirective,
        TableModule,
        Tooltip,
        PaginatorModule,
        LuxonFormatPipe,
        LocalizePipe,
        ViewNotificationModalComponent,
    ],
})
export class NotificationsComponent extends AppComponentBase implements AfterViewInit {
    private _notificationService = inject(NotificationServiceProxy);
    private _userNotificationHelper = inject(UserNotificationHelper);
    private _dateTimeService = inject(DateTimeService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly viewNotificationModal = viewChild<ViewNotificationModalComponent>('viewNotificationModal');

    readStateFilter = signal('ALL');
    dateRange = signal<DateTime[]>([
        this._dateTimeService.getStartOfDay().toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ]);
    loading = signal(false);

    ngAfterViewInit(): void {
        this._userNotificationHelper.viewModal = this.viewNotificationModal();
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    setAsRead(record: any): void {
        this.setNotificationAsRead(record, () => {
            this.reloadPage();
        });
    }

    isRead(record: any): boolean {
        return record.formattedNotification.state === 'READ';
    }

    fromNow(date: DateTime): string {
        return this._dateTimeService.fromNow(date);
    }

    formatRecord(record: any): IFormattedUserNotification {
        return this._userNotificationHelper.format(record, false);
    }

    formatNotification(record: any): string {
        const formattedRecord = this.formatRecord(record);
        return abp.utils.truncateStringWithPostfix(formattedRecord.text, 120);
    }

    formatNotifications(records: any[]): any[] {
        const formattedRecords = [];
        for (const record of records) {
            record.formattedNotification = this.formatRecord(record);
            formattedRecords.push(record);
        }
        return formattedRecords;
    }

    truncateString(text: any, length: number): string {
        return abp.utils.truncateStringWithPostfix(text, length);
    }

    onDateRangeChange(dateRange: DateTime[]): void {
        this.dateRange.set(dateRange);
        if (dateRange && dateRange.length === 2 && dateRange[0] && dateRange[1]) {
            this.getNotifications();
        }
    }

    getNotifications(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        const dateRangeValue = this.dateRange();
        this._notificationService
            .getUserNotifications(
                this.readStateFilter() === 'ALL' ? undefined : UserNotificationState.Unread,
                this._dateTimeService.getStartOfDayForDate(dateRangeValue[0]).toDateTime(),
                this._dateTimeService.getEndOfDayForDate(dateRangeValue[1]).toDateTime(),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                this.primengTableHelper.getSkipCount(this.paginator(), event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(this.formatNotifications(result.items));
                this.primengTableHelper.hideLoadingIndicator();
            });
    }
    setAllNotificationsAsRead(): void {
        this._userNotificationHelper.setAllAsRead(() => {
            this.getNotifications();
        });
    }

    openNotificationSettingsModal(): void {
        this._userNotificationHelper.openSettingsModal();
    }

    setNotificationAsRead(userNotification: UserNotification, callback: () => void): void {
        this._userNotificationHelper.setAsRead(userNotification.id, () => {
            if (callback) {
                callback();
            }
        });
    }

    viewNotification(userNotification: UserNotification): void {
        this._userNotificationHelper.openViewNotificationModal(userNotification.id);
    }

    /**
     * Follow the notification's link through the Router rather than as a plain href, which reloaded the
     * whole application to reach another screen of it. Same helper the bell and the toast use.
     */
    gotoUrl(url: string): void {
        this._userNotificationHelper.navigateToUrl(url);
    }

    deleteNotification(userNotification: UserNotification): void {
        this.message.confirm(this.l('NotificationDeleteWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._notificationService.deleteNotification(userNotification.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    deleteNotifications() {
        this.message.confirm(this.l('DeleteListedNotificationsWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                const dateRangeValue = this.dateRange();
                this._notificationService
                    .deleteAllUserNotifications(
                        this.readStateFilter() === 'ALL' ? undefined : UserNotificationState.Unread,
                        this._dateTimeService.getStartOfDayForDate(dateRangeValue[0]).toDateTime(),
                        this._dateTimeService.getEndOfDayForDate(dateRangeValue[1]).endOf('day').toDateTime()
                    )
                    .subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
            }
        });
    }

    public getRowClass(formattedRecord: IFormattedUserNotification): string {
        let readState = UserNotificationState.Read as any;
        return formattedRecord.state === readState ? 'notification-read text-muted' : '';
    }

    getNotificationTextBySeverity(severity: abp.notifications.severity): string {
        switch (severity) {
            case abp.notifications.severity.SUCCESS:
                return this.l('Success');
            case abp.notifications.severity.WARN:
                return this.l('Warning');
            case abp.notifications.severity.ERROR:
                return this.l('Error');
            case abp.notifications.severity.FATAL:
                return this.l('Fatal');
            case abp.notifications.severity.INFO:
            default:
                return this.l('Info');
        }
    }
}
