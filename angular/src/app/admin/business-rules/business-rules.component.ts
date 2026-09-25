import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BusinessRuleDto,
    BusinessRulesServiceProxy,
    GetAllBusinessRulesInput,
    RuleEventType,
    RulePurpose,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { DateTime } from 'luxon';
import { CreateOrEditBusinessRuleModalComponent } from './create-or-edit-business-rule-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

/**
 * The rule list -- HCMv3's equivalent of the SuccessFactors Business Rules admin screen.
 *
 * The two columns that matter are not the name and the code. They are ASSIGNED, which says whether a rule
 * actually runs anywhere, and the As Of Date filter, which says whether it runs TODAY. A rule list without
 * those answers is a list of things that may or may not be affecting production, which is the state this
 * screen exists to end.
 */
@Component({
    templateUrl: './business-rules.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        DatePipe,
        CreateOrEditBusinessRuleModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class BusinessRulesComponent extends AppComponentBase implements OnInit {
    private _service = inject(BusinessRulesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditBusinessRuleModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly baseObjectFilter = signal<string>('');
    readonly isActiveFilter = signal<boolean | undefined>(undefined);

    /**
     * Defaults to today, matching the SuccessFactors screen. An administrator opening this page is almost
     * always asking "what is running right now", not "what has ever been configured".
     */
    readonly asOfDate = signal<string>(DateTime.now().toISODate());

    readonly baseObjects = signal<{ code: string; displayName: string }[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getRecords().
        this._service.getRuleMetadata().subscribe((metadata) => {
            this.baseObjects.set(metadata.baseObjects.map((b) => ({ code: b.code, displayName: b.displayName })));
        });
    }

    purposeLabel(value: RulePurpose): string {
        return this.l('RulePurpose' + RulePurpose[value]);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getBusinessRules(
                new GetAllBusinessRulesInput({
                    filter: this.filterText(),
                    baseObject: this.baseObjectFilter() || undefined,
                    eventType: undefined,
                    purpose: undefined,
                    isActive: this.isActiveFilter(),
                    asOfDate: this.asOfDate() ? DateTime.fromISO(this.asOfDate()) : undefined,
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
        this.createOrEditModal().show();
    }

    /**
     * Creates the two worked examples, already armed. Confirmed first because it changes behaviour: the
     * probation rule starts deriving dates on the very next employee save.
     */
    installSampleRules(): void {
        this.message.confirm(this.l('InstallSampleRulesWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.installSampleRules().subscribe((created) => {
                    this.reloadPage();

                    if (created.length === 0) {
                        this.notify.info(this.l('SampleRulesAlreadyInstalled'));
                    } else {
                        this.notify.success(this.l('SuccessfullySaved'));
                    }
                });
            }
        });
    }

    showMenu(event: Event, record: BusinessRuleDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    readonly activeMenuItems = signal<MenuItem[]>([]);

    getMenuItems(record: BusinessRuleDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.BusinessRules.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.BusinessRules.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: BusinessRuleDto): void {
        this.message.confirm(this.l('RuleDeleteWarningMessage', record.code), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.deleteBusinessRule(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
