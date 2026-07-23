import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    YearlyTimeRegisterDto,
    YearlyTimeRegistersServiceProxy,
    GetAllYearlyTimeRegistersInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditYearlyTimeRegisterModalComponent } from './create-or-edit-yearly-time-register-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './yearly-time-registers.component.html',
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
        CreateOrEditYearlyTimeRegisterModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class YearlyTimeRegistersComponent extends AppComponentBase implements OnInit {
    private _yearlyTimeRegistersService = inject(YearlyTimeRegistersServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditYearlyTimeRegisterModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getYearlyTimeRegisters().
    }

    getYearlyTimeRegisters(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._yearlyTimeRegistersService
            .getAll(
                new GetAllYearlyTimeRegistersInput({
                    employeeId: undefined,
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
        this.getYearlyTimeRegisters({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createYearlyTimeRegister(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: YearlyTimeRegisterDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: YearlyTimeRegisterDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.TimeManagement.YearlyTimeRegisters.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.TimeManagement.YearlyTimeRegisters.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteYearlyTimeRegister(record),
            });
        }

        return items;
    }

    deleteYearlyTimeRegister(record: YearlyTimeRegisterDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.employeeFullNameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._yearlyTimeRegistersService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
