import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    SimulationTimeRegisterDto,
    SimulationTimeRegistersServiceProxy,
    GetAllSimulationTimeRegistersInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditSimulationTimeRegisterModalComponent } from './create-or-edit-simulation-time-register-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './simulation-time-registers.component.html',
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
        CreateOrEditSimulationTimeRegisterModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class SimulationTimeRegistersComponent extends AppComponentBase implements OnInit {
    private _simulationTimeRegistersService = inject(SimulationTimeRegistersServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditSimulationTimeRegisterModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getSimulationTimeRegisters().
    }

    getSimulationTimeRegisters(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._simulationTimeRegistersService
            .getAll(
                new GetAllSimulationTimeRegistersInput({
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
        this.getSimulationTimeRegisters({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createSimulationTimeRegister(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: SimulationTimeRegisterDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: SimulationTimeRegisterDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.TimeManagement.SimulationTimeRegisters.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.TimeManagement.SimulationTimeRegisters.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteSimulationTimeRegister(record),
            });
        }

        return items;
    }

    deleteSimulationTimeRegister(record: SimulationTimeRegisterDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.employeeFullNameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._simulationTimeRegistersService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
