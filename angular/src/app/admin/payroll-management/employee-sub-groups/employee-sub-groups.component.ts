import { Component, OnInit, inject, input, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeSubGroupDto,
    EmployeeSubGroupsServiceProxy,
    GetAllEmployeeSubGroupsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditEmployeeSubGroupModalComponent } from './create-or-edit-employee-sub-group-modal.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

/**
 * The sub groups of one employee group. Shown as a tab on the group's page -- a sub group always belongs to
 * exactly one group, so that is where it is looked for -- and no longer a screen of its own.
 */
@Component({
    selector: 'employee-sub-groups',
    templateUrl: './employee-sub-groups.component.html',
    imports: [
        BusyIfDirective,
        FormsModule,
        NgClass,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditEmployeeSubGroupModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class EmployeeSubGroupsComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeSubGroupsServiceProxy);
    private _router = inject(Router);

    /** The group whose sub groups are listed. */
    readonly employeeGroupId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeSubGroupModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getEmployeeSubGroups().
    }

    getEmployeeSubGroups(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeSubGroupsInput({
                    filter: this.filterText(),
                    employeeGroupId: this.employeeGroupId(),
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
        this.getEmployeeSubGroups({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createEmployeeSubGroup(): void {
        this.createOrEditModal().show(undefined, this.employeeGroupId());
    }

    viewDetails(record: EmployeeSubGroupDto): void {
        this._router.navigate(['/app/admin/employee-sub-groups', record.id]);
    }

    showMenu(event: Event, record: EmployeeSubGroupDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeSubGroupDto): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Details'),
            icon: 'pi pi-eye',
            command: () => this.viewDetails(record),
        });

        if (this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroups.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroups.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteEmployeeSubGroup(record),
            });
        }

        return items;
    }

    deleteEmployeeSubGroup(record: EmployeeSubGroupDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.nameEn), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
