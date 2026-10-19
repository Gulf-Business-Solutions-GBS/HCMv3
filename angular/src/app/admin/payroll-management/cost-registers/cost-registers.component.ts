import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CostRegisterDto, CostRegistersServiceProxy, GetAllCostRegistersInput } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditCostRegisterModalComponent } from './create-or-edit-cost-register-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { Router, RouterLink } from '@angular/router';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './cost-registers.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditCostRegisterModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        RouterLink,
        LuxonFormatPipe,
    ],
})
export class CostRegistersComponent extends AppComponentBase implements OnInit {
    private _costRegistersService = inject(CostRegistersServiceProxy);
    private _router = inject(Router);

    readonly createOrEditModal = viewChild<CreateOrEditCostRegisterModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getCostRegisters().
    }

    getCostRegisters(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._costRegistersService
            .getAll(
                new GetAllCostRegistersInput({
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
        this.getCostRegisters({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createCostRegister(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: CostRegisterDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: CostRegisterDto): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Details'),
            icon: 'pi pi-list',
            command: () => this._router.navigate(['/app/admin/cost-registers', record.id]),
        });

        if (this.permission.isGranted('Pages.PayrollManagement.CostRegisters.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.CostRegisters.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteCostRegister(record),
            });
        }

        return items;
    }

    deleteCostRegister(record: CostRegisterDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.employeeCode), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._costRegistersService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
