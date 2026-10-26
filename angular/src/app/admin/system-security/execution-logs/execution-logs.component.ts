import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ExecutionLogAction,
    ExecutionLogDto,
    ExecutionLogModule,
    ExecutionLogsServiceProxy,
    ExecutionLogStatus,
    GetAllExecutionLogsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditExecutionLogModalComponent } from './create-or-edit-execution-log-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './execution-logs.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditExecutionLogModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class ExecutionLogsComponent extends AppComponentBase implements OnInit {
    private _service = inject(ExecutionLogsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditExecutionLogModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    readonly actionNames: Record<number, string> = {
        [ExecutionLogAction.RUN]: 'RUN',
        [ExecutionLogAction.GENERATE]: 'GENERATE',
        [ExecutionLogAction.VIEW]: 'VIEW',
        [ExecutionLogAction.EXPORT]: 'EXPORT',
        [ExecutionLogAction.PRINT]: 'PRINT',
    };

    readonly moduleNames: Record<number, string> = {
        [ExecutionLogModule.TimeEngine]: 'TimeEngine',
        [ExecutionLogModule.PayrollEngine]: 'PayrollEngine',
        [ExecutionLogModule.BankFile]: 'BankFile',
        [ExecutionLogModule.GridReport]: 'GridReport',
        [ExecutionLogModule.PrintReport]: 'PrintReport',
        [ExecutionLogModule.CostEngine]: 'CostEngine',
        [ExecutionLogModule.ProjectEngine]: 'ProjectEngine',
    };

    readonly statusNames: Record<number, string> = {
        [ExecutionLogStatus.INPRECESS]: 'INPRECESS',
        [ExecutionLogStatus.SUCCESS]: 'SUCCESS',
        [ExecutionLogStatus.FAILED]: 'FAILED',
        [ExecutionLogStatus.PARTIAL]: 'PARTIAL',
        [ExecutionLogStatus.ABORTED]: 'ABORTED',
    };

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
                new GetAllExecutionLogsInput({
                    module: undefined as any,
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

    showMenu(event: Event, record: ExecutionLogDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: ExecutionLogDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.SystemSecurity.ExecutionLogs.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.SystemSecurity.ExecutionLogs.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: ExecutionLogDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.screenName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._service.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
