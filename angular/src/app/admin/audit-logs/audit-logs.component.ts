import { Component, ViewEncapsulation, AfterViewInit, inject, viewChild, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuditLogDetailModalComponent } from '@app/admin/audit-logs/audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent } from '@app/shared/common/entityHistory/entity-change-detail-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AuditLogListDto,
    AuditLogServiceProxy,
    EntityChangeListDto,
    NameValueDto,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { PrimengTableHelper } from 'shared/helpers/PrimengTableHelper';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/primeng-date-range-picker-luxon-modifier.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AuditLogDetailModalComponent as AuditLogDetailModalComponent_1 } from './audit-log-detail-modal.component';
import { EntityChangeDetailModalComponent as EntityChangeDetailModalComponent_1 } from '../../shared/common/entityHistory/entity-change-detail-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ButtonModule } from 'primeng/button';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';

@Component({
    templateUrl: './audit-logs.component.html',
    styleUrls: ['./audit-logs.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        DatePickerModule,
        PrimengDateRangePickerLuxonModifierDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        ButtonModule,
        TieredMenu,
        AuditLogDetailModalComponent_1,
        EntityChangeDetailModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
    ],
})
export class AuditLogsComponent extends AppComponentBase implements AfterViewInit {
    private readonly _auditLogService = inject(AuditLogServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);
    private readonly _dateTimeService = inject(DateTimeService);
    private readonly _router = inject(Router);

    readonly auditLogDetailModal = viewChild<AuditLogDetailModalComponent>('auditLogDetailModal');
    readonly entityChangeDetailModal = viewChild<EntityChangeDetailModalComponent>('entityChangeDetailModal');
    readonly dataTableAuditLogs = viewChild<Table>('dataTableAuditLogs');
    readonly dataTableEntityChanges = viewChild<Table>('dataTableEntityChanges');
    readonly paginatorAuditLogs = viewChild<Paginator>('paginatorAuditLogs');
    readonly paginatorEntityChanges = viewChild<Paginator>('paginatorEntityChanges');

    readonly dateRange = signal<DateTime[]>([
        this._dateTimeService.getStartOfDay().toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ]);

    readonly usernameAuditLog = signal<string>('');
    readonly usernameEntityChange = signal<string>('');
    readonly serviceName = signal<string>('');
    readonly methodName = signal<string>('');
    readonly browserInfo = signal<string>('');
    readonly hasException = signal<boolean | undefined>(undefined);
    readonly minExecutionDuration = signal<number | undefined>(undefined);
    readonly maxExecutionDuration = signal<number | undefined>(undefined);
    readonly entityTypeFullName = signal<string>('');

    readonly objectTypes = signal<NameValueDto[]>([]);
    readonly advancedFiltersAreShown = signal(false);
    readonly activeEntityChangeMenuItems = signal<MenuItem[]>([]);

    primengTableHelperAuditLogs = new PrimengTableHelper();
    primengTableHelperEntityChanges = new PrimengTableHelper();

    ngAfterViewInit(): void {
        this.primengTableHelperAuditLogs.adjustScroll(this.dataTableAuditLogs());
        this.primengTableHelperEntityChanges.adjustScroll(this.dataTableEntityChanges());
    }

    onAuditLogsDateRangeChange(dateRange: DateTime[]): void {
        this.dateRange.set(dateRange);
        if (dateRange && dateRange.length === 2 && dateRange[0] && dateRange[1]) {
            this.getAuditLogs();
        }
    }

    onEntityChangesDateRangeChange(dateRange: DateTime[]): void {
        this.dateRange.set(dateRange);
        if (dateRange && dateRange.length === 2 && dateRange[0] && dateRange[1]) {
            this.getEntityChanges();
        }
    }

    showAuditLogDetails(record: AuditLogListDto): void {
        this.auditLogDetailModal().show(record);
    }

    showEntityChangeDetails(record: EntityChangeListDto): void {
        this.entityChangeDetailModal().show(record);
    }

    showEntityChangeMenu(event: Event, record: EntityChangeListDto, menu: any): void {
        this.activeEntityChangeMenuItems.set(this.getMenuItemsForEntityChange(record));
        menu.toggle(event);
    }

    getMenuItemsForEntityChange(record: EntityChangeListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Administration.EntityChanges.FullHistory')) {
            items.push({
                label: this.l('AllChanges'),
                icon: 'pi pi-list',
                command: () => this.showEntityChangeAllDetails(record),
            });
        }

        items.push({
            label: this.l('ViewChange'),
            icon: 'pi pi-eye',
            command: () => this.showEntityChangeDetails(record),
        });

        return items;
    }

    getAuditLogs(event?: LazyLoadEvent) {
        if (this.primengTableHelperAuditLogs.shouldResetPaging(event)) {
            this.paginatorAuditLogs().changePage(0);

            if (this.primengTableHelperAuditLogs.records() && this.primengTableHelperAuditLogs.records().length > 0) {
                return;
            }
        }

        this.primengTableHelperAuditLogs.showLoadingIndicator();

        const currentDateRange = this.dateRange();
        this._auditLogService
            .getAuditLogs(
                this._dateTimeService.getStartOfDayForDate(currentDateRange[0]).toDateTime(),
                this._dateTimeService.getEndOfDayForDate(currentDateRange[1]).toDateTime(),
                this.usernameAuditLog(),
                this.serviceName(),
                this.methodName(),
                this.browserInfo(),
                this.hasException(),
                this.minExecutionDuration(),
                this.maxExecutionDuration(),
                this.primengTableHelperAuditLogs.getSorting(this.dataTableAuditLogs()),
                this.primengTableHelperAuditLogs.getSkipCount(this.paginatorAuditLogs(), event),
                this.primengTableHelperAuditLogs.getMaxResultCount(this.paginatorAuditLogs(), event)
            )
            .subscribe((result) => {
                this.primengTableHelperAuditLogs.totalRecordsCount.set(result.totalCount);
                this.primengTableHelperAuditLogs.records.set(result.items);
                this.primengTableHelperAuditLogs.hideLoadingIndicator();
            });
    }

    getEntityChanges(event?: LazyLoadEvent) {
        if (this.objectTypes().length === 0) {
            this._auditLogService.getEntityHistoryObjectTypes().subscribe((result) => {
                this.objectTypes.set(result);
            });
        }

        if (this.primengTableHelperEntityChanges.shouldResetPaging(event)) {
            this.paginatorEntityChanges().changePage(0);

            if (
                this.primengTableHelperEntityChanges.records() &&
                this.primengTableHelperEntityChanges.records().length > 0
            ) {
                return;
            }
        }

        this.primengTableHelperEntityChanges.showLoadingIndicator();
        const currentDateRange = this.dateRange();
        this._auditLogService
            .getEntityChanges(
                this._dateTimeService.getStartOfDayForDate(currentDateRange[0]).toDateTime(),
                this._dateTimeService.getEndOfDayForDate(currentDateRange[1]).toDateTime(),
                this.usernameEntityChange(),
                this.entityTypeFullName(),
                this.primengTableHelperEntityChanges.getSorting(this.dataTableEntityChanges()),
                this.primengTableHelperEntityChanges.getSkipCount(this.paginatorEntityChanges(), event),
                this.primengTableHelperEntityChanges.getMaxResultCount(this.paginatorEntityChanges(), event)
            )
            .subscribe((result) => {
                this.primengTableHelperEntityChanges.totalRecordsCount.set(result.totalCount);
                this.primengTableHelperEntityChanges.records.set(result.items);
                this.primengTableHelperEntityChanges.hideLoadingIndicator();
            });
    }

    showEntityChangeAllDetails(record: EntityChangeListDto): void {
        this._router.navigate([
            abp.appPath + `/app/admin/entity-changes/${record.entityId}/${record.entityTypeFullName}`,
        ]);
    }

    exportToExcelAuditLogs(): void {
        const self = this;
        const currentDateRange = this.dateRange();
        self._auditLogService
            .getAuditLogsToExcel(
                this._dateTimeService.getStartOfDayForDate(currentDateRange[0]).toDateTime(),
                this._dateTimeService.getEndOfDayForDate(currentDateRange[1]).toDateTime(),
                self.usernameAuditLog(),
                self.serviceName(),
                self.methodName(),
                self.browserInfo(),
                self.hasException(),
                self.minExecutionDuration(),
                self.maxExecutionDuration(),
                undefined,
                1,
                0
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }

    exportToExcelEntityChanges(): void {
        const self = this;
        const currentDateRange = this.dateRange();
        self._auditLogService
            .getEntityChangesToExcel(
                this._dateTimeService.getStartOfDayForDate(currentDateRange[0]).toDateTime(),
                self._dateTimeService.getEndOfDayForDate(currentDateRange[1]).toDateTime(),
                self.usernameEntityChange(),
                self.entityTypeFullName(),
                undefined,
                1,
                0
            )
            .subscribe((result) => {
                self._fileDownloadService.downloadTempFile(result);
            });
    }

    truncateStringWithPostfix(text: string, length: number): string {
        return abp.utils.truncateStringWithPostfix(text, length);
    }
}
