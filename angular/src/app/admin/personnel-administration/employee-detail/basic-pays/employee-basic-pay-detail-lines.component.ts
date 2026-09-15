import { Component, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DependantUnit,
    EmployeeBasicPayDetailDto,
    EmployeeBasicPayDetailsServiceProxy,
    GetAllEmployeeBasicPayDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { MenuItem } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditEmployeeBasicPayDetailModalComponent } from '../../employee-basic-pay-details/create-or-edit-employee-basic-pay-detail-modal.component';

/** The wage type lines of one basic pay record, shown inside its expanded row. */
@Component({
    selector: 'employee-basic-pay-detail-lines',
    templateUrl: './employee-basic-pay-detail-lines.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        TieredMenu,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeBasicPayDetailModalComponent,
    ],
})
export class EmployeeBasicPayDetailLinesComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPayDetailsServiceProxy);

    readonly employeeBasicPayId = input<number>();

    /** Emitted after a line is saved or deleted, so the header (whose amount is the sum of its lines) can reload. */
    readonly changed = output<void>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeBasicPayDetailModalComponent>('createOrEditDetailModal');

    readonly lines = signal<EmployeeBasicPayDetailDto[]>([]);
    readonly loading = signal<boolean>(false);
    readonly loaded = signal<boolean>(false);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        // Plain (non-lazy) table, so loading from the input is the only trigger.
        effect(() => {
            if (this.employeeBasicPayId()) {
                this.getLines();
            }
        });
    }

    getLines(): void {
        const employeeBasicPayId = this.employeeBasicPayId();
        if (!employeeBasicPayId) {
            return;
        }

        this.loading.set(true);
        this._service
            .getAll(
                new GetAllEmployeeBasicPayDetailsInput({
                    employeeBasicPayId,
                    sorting: undefined,
                    skipCount: 0,
                    maxResultCount: 1000,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.lines.set(result.items);
                this.loaded.set(true);
            });
    }

    createLine(): void {
        this.createOrEditModal()?.show(undefined, this.employeeBasicPayId());
    }

    onLineSaved(): void {
        this.getLines();
        this.changed.emit();
    }

    dependantUnitName(unit: DependantUnit): string {
        const name = DependantUnit[unit];
        return name ? this.l(name) : '';
    }

    showMenu(event: Event, record: EmployeeBasicPayDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeBasicPayDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPayDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal()?.show(record.id, this.employeeBasicPayId()),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPayDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteLine(record),
            });
        }

        return items;
    }

    deleteLine(record: EmployeeBasicPayDetailDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this.loading.set(true);
                this._service
                    .delete(record.id)
                    .pipe(finalize(() => this.loading.set(false)))
                    .subscribe(() => {
                        this.notify.success(this.l('SuccessfullyDeleted'));
                        this.onLineSaved();
                    });
            }
        });
    }
}
