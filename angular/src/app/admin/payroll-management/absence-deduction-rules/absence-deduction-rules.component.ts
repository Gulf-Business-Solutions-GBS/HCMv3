import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDeductionRuleDto,
    AbsenceDeductionRulesServiceProxy,
    GetAllAbsenceDeductionRulesInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CreateOrEditAbsenceDeductionRuleModalComponent } from './create-or-edit-absence-deduction-rule-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './absence-deduction-rules.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        RouterLink,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditAbsenceDeductionRuleModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class AbsenceDeductionRulesComponent extends AppComponentBase implements OnInit {
    private _absenceDeductionRulesService = inject(AbsenceDeductionRulesServiceProxy);
    private _router = inject(Router);

    readonly createOrEditModal = viewChild<CreateOrEditAbsenceDeductionRuleModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getAbsenceDeductionRules().
    }

    getAbsenceDeductionRules(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._absenceDeductionRulesService
            .getAll(
                new GetAllAbsenceDeductionRulesInput({
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
        this.getAbsenceDeductionRules({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createAbsenceDeductionRule(): void {
        this.createOrEditModal().show();
    }

    /** Opens the record read-only. */
    viewDetails(record: AbsenceDeductionRuleDto): void {
        this._router.navigate(['/app/admin/absence-deduction-rules', record.id]);
    }

    /** Opens the same page with its fields already unlocked. */
    editAbsenceDeductionRule(record: AbsenceDeductionRuleDto): void {
        this._router.navigate(['/app/admin/absence-deduction-rules', record.id], { queryParams: { edit: true } });
    }

    showMenu(event: Event, record: AbsenceDeductionRuleDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: AbsenceDeductionRuleDto): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Details'),
            icon: 'pi pi-eye',
            command: () => this.viewDetails(record),
        });

        if (this.permission.isGranted('Pages.PayrollManagement.AbsenceDeductionRules.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.editAbsenceDeductionRule(record),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.AbsenceDeductionRules.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteAbsenceDeductionRule(record),
            });
        }

        return items;
    }

    deleteAbsenceDeductionRule(record: AbsenceDeductionRuleDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.ruleName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._absenceDeductionRulesService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
