import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeSubGroupBenefitDto,
    EmployeeSubGroupBenefitsServiceProxy,
    GetAllEmployeeSubGroupBenefitsInput,
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
import { CreateOrEditEmployeeSubGroupBenefitModalComponent } from './create-or-edit-employee-sub-group-benefit-modal.component';
import { EmployeeSubGroupBenefitDetailsComponent } from './benefit-details/employee-sub-group-benefit-details.component';

@Component({
    selector: 'employee-sub-group-benefits',
    templateUrl: './employee-sub-group-benefits.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeSubGroupBenefitModalComponent,
        EmployeeSubGroupBenefitDetailsComponent,
    ],
})
export class EmployeeSubGroupBenefitsComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupBenefitsServiceProxy);

    readonly employeeSubGroupId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeSubGroupBenefitModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly selectedBenefit = signal<EmployeeSubGroupBenefitDto | undefined>(undefined);

    constructor() {
        super();
        effect(() => {
            if (this.employeeSubGroupId()) {
                this.reloadPage();
            }
        });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.employeeSubGroupId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeSubGroupBenefitsInput({
                    employeeSubGroupId: this.employeeSubGroupId(),
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
        const rows = this.paginator()
            ? this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage
            : this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: (this.paginator() ? this.paginator().getPage() : 0) * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(this.employeeSubGroupId());
    }

    manageDetails(record: EmployeeSubGroupBenefitDto): void {
        this.selectedBenefit.set(record);
    }

    showMenu(event: Event, record: EmployeeSubGroupBenefitDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeSubGroupBenefitDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroupBenefitDetails')) {
            items.push({
                label: this.l('EmployeeSubGroupBenefitDetails'),
                icon: 'pi pi-list',
                command: () => this.manageDetails(record),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroupBenefits.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.employeeSubGroupId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroupBenefits.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeSubGroupBenefitDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    if (this.selectedBenefit()?.id === record.id) {
                        this.selectedBenefit.set(undefined);
                    }
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
