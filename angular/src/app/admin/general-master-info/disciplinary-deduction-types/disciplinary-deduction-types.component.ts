import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DisciplinaryDeductionTypeDto,
    DisciplinaryDeductionTypesServiceProxy,
    GetAllDisciplinaryDeductionTypesInput,
    DisciplinaryDeductionUnit,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditDisciplinaryDeductionTypeModalComponent } from './create-or-edit-disciplinary-deduction-type-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './disciplinary-deduction-types.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditDisciplinaryDeductionTypeModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class DisciplinaryDeductionTypesComponent extends AppComponentBase implements OnInit {
    private _disciplinaryDeductionTypesService = inject(DisciplinaryDeductionTypesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditDisciplinaryDeductionTypeModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getDisciplinaryDeductionTypes().
    }

    getDisciplinaryDeductionTypes(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._disciplinaryDeductionTypesService
            .getAll(
                new GetAllDisciplinaryDeductionTypesInput({
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
        this.paginator().changePage(this.paginator().getPage());
    }

    createDisciplinaryDeductionType(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: DisciplinaryDeductionTypeDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: DisciplinaryDeductionTypeDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteDisciplinaryDeductionType(record),
            });
        }

        return items;
    }

    deductionUnitLabel(unit: DisciplinaryDeductionUnit): string {
        switch (unit) {
            case DisciplinaryDeductionUnit.Hours:
                return this.l('DisciplinaryDeductionUnitHours');
            case DisciplinaryDeductionUnit.FixedAmount:
                return this.l('DisciplinaryDeductionUnitFixedAmount');
            case DisciplinaryDeductionUnit.Percentage:
                return this.l('DisciplinaryDeductionUnitPercentage');
            default:
                return this.l('DisciplinaryDeductionUnitDays');
        }
    }

    deleteDisciplinaryDeductionType(record: DisciplinaryDeductionTypeDto): void {
        this.message.confirm(
            this.l('DisciplinaryDeductionTypeDeleteWarningMessage', record.nameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._disciplinaryDeductionTypesService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
