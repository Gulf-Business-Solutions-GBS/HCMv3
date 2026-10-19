import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GetLicenceAssignmentSummaryOutput,
    GetLicenceAssignmentUsersInput,
    LicenceAssignmentServiceProxy,
    LicenceAssignmentUserDto,
    LicenceModule,
    LicenceModuleUsageDto,
    SetUserModuleLicenceInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Licence Assignment (Administration > Tools). Legacy uscLicenseAssignment, on ASP.NET Zero editions: one tick
 * per user and module, "used / available" per module, and a tick refused when the module has no seat left.
 *
 * Seat counts and which modules are in the edition are set by the host on the tenant's edition (Zero's Editions
 * and Tenants screens); this screen only hands the seats out. Administrators need no licence and are not counted.
 */
@Component({
    templateUrl: './licence-assignment.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, PaginatorModule, LocalizePipe],
})
export class LicenceAssignmentComponent extends AppComponentBase implements OnInit {
    private _service = inject(LicenceAssignmentServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly moduleFilter = signal<LicenceModule | undefined>(undefined);
    /** undefined = everyone; true = holds the filtered module; false = does not. */
    readonly assignedFilter = signal<boolean | undefined>(undefined);
    readonly summary = signal<GetLicenceAssignmentSummaryOutput | undefined>(undefined);
    /** "userId:module" of the tick being saved, so only that checkbox is disabled while it saves. */
    readonly savingKey = signal<string | undefined>(undefined);

    ngOnInit(): void {
        // The PrimeNG table's (onLazyLoad) loads the first page of users; only the header is fetched here.
        this.loadSummary();
    }

    loadSummary(): void {
        this._service.getSummary().subscribe((s) => this.summary.set(s));
    }

    modules(): LicenceModuleUsageDto[] {
        return this.summary()?.modules ?? [];
    }

    getUsers(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const module = this.moduleFilter();
        this._service
            .getUsers(
                new GetLicenceAssignmentUsersInput({
                    filter: this.filterText(),
                    module: module,
                    isAssigned: module === undefined ? undefined : this.assignedFilter(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((r) => {
                this.primengTableHelper.totalRecordsCount.set(r.totalCount);
                this.primengTableHelper.records.set(r.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    /** A new filter starts again from the first page. */
    reloadPage(): void {
        if (this.paginator().getPage() !== 0) {
            this.paginator().changePage(0);
            return;
        }

        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getUsers({ first: 0, rows } as LazyLoadEvent);
    }

    onModuleFilterChange(module: LicenceModule | undefined): void {
        this.moduleFilter.set(module);
        if (module === undefined) {
            this.assignedFilter.set(undefined);
        }
        this.reloadPage();
    }

    onAssignedFilterChange(value: boolean | undefined): void {
        this.assignedFilter.set(value);
        this.reloadPage();
    }

    holds(record: LicenceAssignmentUserDto, module: LicenceModule): boolean {
        return (record.modules ?? []).includes(module);
    }

    /** Text for the "used / available" figure of one module. */
    usageText(usage: LicenceModuleUsageDto): string {
        return usage.isUnlimited
            ? this.l('LicenceUsedOfUnlimited', usage.usedCount)
            : this.l('LicenceUsedOfTotal', usage.usedCount, usage.seatCount);
    }

    /**
     * Why a tick cannot be changed, or undefined when it can. Unticking is always allowed; ticking needs the module in
     * the edition and a free seat. The server enforces the same rules -- this only saves a round trip.
     */
    lockReason(record: LicenceAssignmentUserDto, usage: LicenceModuleUsageDto): string | undefined {
        if (record.isAdministrator) {
            return this.l('LicenceNotNeededForAdministratorShort');
        }

        if (this.holds(record, usage.module)) {
            return undefined;
        }

        if (!usage.isModuleEnabled) {
            return this.l('LicenceModuleNotInEditionShort');
        }

        if (!usage.isUnlimited && usage.usedCount >= usage.seatCount) {
            return this.l('LicenceNoSeatLeftShort');
        }

        return undefined;
    }

    isSaving(record: LicenceAssignmentUserDto, module: LicenceModule): boolean {
        return this.savingKey() === `${record.id}:${module}`;
    }

    toggle(record: LicenceAssignmentUserDto, usage: LicenceModuleUsageDto, checkbox: HTMLInputElement): void {
        const isAssigned = checkbox.checked;
        this.savingKey.set(`${record.id}:${usage.module}`);

        this._service
            .setUserModuleLicence(
                new SetUserModuleLicenceInput({ userId: record.id, module: usage.module, isAssigned: isAssigned })
            )
            .pipe(finalize(() => this.savingKey.set(undefined)))
            .subscribe({
                next: (updated) => {
                    const held = (record.modules ?? []).filter((m) => m !== usage.module);
                    record.modules = isAssigned ? [...held, usage.module] : held;
                    this.primengTableHelper.records.set([...this.primengTableHelper.records()]);
                    this.replaceUsage(updated);
                    this.notify.success(this.l('SavedSuccessfully'));
                },
                error: () => {
                    // Refused (no seat left, module switched off by the host meanwhile): put the tick back and
                    // refresh the counts, which another administrator may have moved.
                    checkbox.checked = !isAssigned;
                    this.loadSummary();
                },
            });
    }

    private replaceUsage(updated: LicenceModuleUsageDto): void {
        const current = this.summary();
        if (!current) {
            return;
        }

        current.modules = current.modules.map((m) => (m.module === updated.module ? updated : m));
        this.summary.set(GetLicenceAssignmentSummaryOutput.fromJS(current.toJSON()));
    }
}
