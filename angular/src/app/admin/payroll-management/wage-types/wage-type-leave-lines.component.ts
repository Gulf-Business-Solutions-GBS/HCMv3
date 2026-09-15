import { Component, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetAllWageTypeLeavesInput, WageTypeLeaveDto, WageTypeLeavesServiceProxy } from '@shared/service-proxies/service-proxies';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { finalize } from 'rxjs/operators';
import { CreateOrEditWageTypeLeaveModalComponent } from '../wage-type-leaves/create-or-edit-wage-type-leave-modal.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

/**
 * Detail lines shown when a wage type row is expanded on the Wage Types screen:
 * the absence types whose leave pay / provision uses this wage type.
 */
@Component({
    selector: 'wageTypeLeaveLines',
    templateUrl: './wage-type-leave-lines.component.html',
    imports: [BusyIfDirective, TieredMenu, CreateOrEditWageTypeLeaveModalComponent, LocalizePipe, PermissionPipe, PermissionAnyPipe],
})
export class WageTypeLeaveLinesComponent extends AppComponentBase implements OnInit {
    private _wageTypeLeavesService = inject(WageTypeLeavesServiceProxy);

    readonly wageTypeId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditWageTypeLeaveModalComponent>('createOrEditLeaveModal');

    readonly loading = signal<boolean>(false);
    readonly lines = signal<WageTypeLeaveDto[]>([]);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // No lazy-loaded table here: the lines are loaded once when the parent row is expanded.
        this.getLines();
    }

    getLines(): void {
        this.loading.set(true);

        this._wageTypeLeavesService
            .getAll(
                new GetAllWageTypeLeavesInput({
                    wageTypeId: this.wageTypeId(),
                    sorting: undefined,
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.lines.set(result.items);
            });
    }

    createLine(): void {
        this.createOrEditModal().show(undefined, this.wageTypeId());
    }

    showMenu(event: Event, record: WageTypeLeaveDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: WageTypeLeaveDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeLeaves.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.wageTypeId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.WageTypeLeaves.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteLine(record),
            });
        }

        return items;
    }

    deleteLine(record: WageTypeLeaveDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.absenceTypeName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._wageTypeLeavesService.delete(record.id).subscribe(() => {
                    this.getLines();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
