import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutyPeriodsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditDutyPeriodModalComponent } from './create-or-edit-duty-period-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './duty-periods.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        NgClass,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditDutyPeriodModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class DutyPeriodsComponent extends AppComponentBase implements OnInit {
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditDutyPeriodModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getDutyPeriods().
    }

    getDutyPeriods(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._dutyPeriodsService
            .getAll(
                new GetAllDutyPeriodsInput({
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
        this.getDutyPeriods({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createDutyPeriod(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: DutyPeriodDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: DutyPeriodDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.TimeManagement.DutyPeriods.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.TimeManagement.DutyPeriods.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteDutyPeriod(record),
            });
        }

        return items;
    }

    deleteDutyPeriod(record: DutyPeriodDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.nameEn), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._dutyPeriodsService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
