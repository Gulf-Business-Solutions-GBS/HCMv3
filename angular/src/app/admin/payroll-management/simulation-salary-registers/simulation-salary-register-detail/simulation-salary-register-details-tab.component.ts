import { Component, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllSimulationSalaryRegisterDetailsInput,
    SimulationSalaryRegisterDetailDto,
    SimulationSalaryRegisterDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditSimulationSalaryRegisterDetailModalComponent } from '../../simulation-salary-register-details/create-or-edit-simulation-salary-register-detail-modal.component';

/** Details tab of the simulation salary register page: the wage type lines of this register only. */
@Component({
    selector: 'simulationSalaryRegisterDetailsTab',
    templateUrl: './simulation-salary-register-details-tab.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditSimulationSalaryRegisterDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class SimulationSalaryRegisterDetailsTabComponent extends AppComponentBase {
    private _detailsService = inject(SimulationSalaryRegisterDetailsServiceProxy);

    readonly simulationSalaryRegisterId = input.required<number>();

    readonly createOrEditModal =
        viewChild<CreateOrEditSimulationSalaryRegisterDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    // No getRecords() in ngOnInit: the PrimeNG table's (onLazyLoad) triggers the first load.

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._detailsService
            .getAll(
                new GetAllSimulationSalaryRegisterDetailsInput({
                    simulationSalaryRegisterId: this.simulationSalaryRegisterId(),
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
        this.createOrEditModal().show(undefined, this.simulationSalaryRegisterId());
    }

    showMenu(event: Event, record: SimulationSalaryRegisterDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: SimulationSalaryRegisterDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.SimulationSalaryRegisterDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.simulationSalaryRegisterId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.SimulationSalaryRegisterDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: SimulationSalaryRegisterDetailDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.wageTypeName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._detailsService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
