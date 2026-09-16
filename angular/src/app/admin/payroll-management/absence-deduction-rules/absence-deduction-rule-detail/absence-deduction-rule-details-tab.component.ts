import { Component, DestroyRef, OnInit, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDedRuleDetailDto,
    AbsenceDedRuleDetailsServiceProxy,
    DeductionUnit,
    GetAllAbsenceDedRuleDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditAbsenceDedRuleDetailModalComponent } from '../../absence-ded-rule-details/create-or-edit-absence-ded-rule-detail-modal.component';

/**
 * Details tab of the absence deduction rule page: the dated deduction lines of this rule.
 *
 * The backend filter matches by value, not text: a number matches days count or deduction number,
 * a yyyy-MM-dd date matches lines whose period includes it, a 4-digit year matches lines touching that year.
 */
@Component({
    selector: 'absenceDeductionRuleDetailsTab',
    templateUrl: './absence-deduction-rule-details-tab.component.html',
    imports: [
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditAbsenceDedRuleDetailModalComponent,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class AbsenceDeductionRuleDetailsTabComponent extends AppComponentBase implements OnInit {
    private _absenceDedRuleDetailsService = inject(AbsenceDedRuleDetailsServiceProxy);
    private _destroyRef = inject(DestroyRef);

    readonly absenceDeductionRuleId = input.required<number>();

    readonly createOrEditModal = viewChild<CreateOrEditAbsenceDedRuleDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** Localization keys of the deduction unit names, as the Create/Edit modal shows them. */
    private readonly _deductionUnitKeys: Record<number, string> = {
        [DeductionUnit.Fixed]: 'Fixed',
        [DeductionUnit.Percentage]: 'Percentage',
    };

    private readonly _filterChanged = new Subject<string>();

    ngOnInit(): void {
        // No getRecords() here: the PrimeNG table's (onLazyLoad) triggers the first load.
        this._filterChanged
            .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.getRecords());
    }

    onFilterChange(value: string): void {
        this.filterText.set(value);
        this._filterChanged.next(value);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._absenceDedRuleDetailsService
            .getAll(
                new GetAllAbsenceDedRuleDetailsInput({
                    absenceDeductionRuleId: this.absenceDeductionRuleId(),
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
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    deductionUnitName(unit: DeductionUnit): string {
        const key = this._deductionUnitKeys[unit];
        return key ? this.l(key) : '';
    }

    createRecord(): void {
        this.createOrEditModal().show(undefined, this.absenceDeductionRuleId());
    }

    showMenu(event: Event, record: AbsenceDedRuleDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: AbsenceDedRuleDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.AbsenceDedRuleDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id, this.absenceDeductionRuleId()),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.AbsenceDedRuleDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: AbsenceDedRuleDetailDto): void {
        // Lines have no name of their own; the period identifies one within its rule.
        const period = `${record.startDate?.toISODate() ?? ''} - ${record.endDate?.toISODate() ?? ''}`;
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', period), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._absenceDedRuleDetailsService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
