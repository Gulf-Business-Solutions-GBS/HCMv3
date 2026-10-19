import { NgClass } from '@angular/common';
import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NotificationMasterDto, NotificationMastersServiceProxy, GetAllNotificationMastersInput } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditNotificationMasterModalComponent } from './create-or-edit-notification-master-modal.component';
import { NotificationSentLogModalComponent } from './notification-sent-log-modal.component';
import { printFormula } from '../../business-rules/rule-formula';
import { RuleExpression } from '../../business-rules/rule-model';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './notification-masters.component.html',
    animations: [appModuleAnimation()],
    imports: [
        NgClass,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditNotificationMasterModalComponent,
        NotificationSentLogModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class NotificationMastersComponent extends AppComponentBase implements OnInit {
    private _service = inject(NotificationMastersServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditNotificationMasterModalComponent>('createOrEditModal');
    readonly sentLogModal = viewChild<NotificationSentLogModalComponent>('sentLogModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getRecords().
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllNotificationMastersInput({
                    filter: this.filterText(),
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

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: NotificationMasterDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    /** The stored criterion as the formula the administrator typed; legacy text is shown as it is. */
    criterionText(record: NotificationMasterDto): string {
        if (!record.criterion) {
            return this.l('NotificationCriterionAllRecords');
        }

        try {
            return printFormula(JSON.parse(record.criterion) as RuleExpression);
        } catch {
            return record.criterion;
        }
    }

    showSentLog(record?: NotificationMasterDto): void {
        this.sentLogModal().show(record?.id, record?.notificationTypeName);
    }

    getMenuItems(record: NotificationMasterDto): MenuItem[] {
        const items: MenuItem[] = [
            {
                label: this.l('NotificationSentHistory'),
                icon: 'pi pi-history',
                command: () => this.showSentLog(record),
            },
        ];

        if (this.permission.isGranted('Pages.SystemSecurity.NotificationMasters.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.SystemSecurity.NotificationMasters.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: NotificationMasterDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.notificationType), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
