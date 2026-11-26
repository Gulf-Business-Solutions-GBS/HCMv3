import { Component, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeOrgAssignmentDto,
    EmployeeOrgAssignmentFieldSettingsDto,
    EmployeeOrgAssignmentsServiceProxy,
    GetAllEmployeeOrgAssignmentsInput,
    OrgAssignmentOrgUnitDto,
} from '@shared/service-proxies/service-proxies';
import { injectOrgLevelCaptions } from '@app/admin/reports/analysis/shared/org-level-captions';
import { orgLevelPath } from './org-level-path';

const ORG_LEVELS = [1, 2, 3, 4, 5, 6, 7];
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditEmployeeOrgAssignmentModalComponent } from './create-or-edit-employee-org-assignment-modal.component';

@Component({
    selector: 'employee-org-assignments',
    templateUrl: './employee-org-assignments.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeOrgAssignmentModalComponent,
    ],
})
export class EmployeeOrgAssignmentsComponent extends AppComponentBase {
    private _service = inject(EmployeeOrgAssignmentsServiceProxy);

    readonly employeeId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeOrgAssignmentModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** Legacy OrgUnitCustomizeController: Position, Posting Group and Budgeting Group columns follow Org Settings. */
    readonly fieldSettings = signal<EmployeeOrgAssignmentFieldSettingsDto>(new EmployeeOrgAssignmentFieldSettingsDto());

    /** Legacy OrgLevel1..OrgLevel7 columns, captioned with the tenant's Organization Unit Type names. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly shownOrgLevels = computed(() => ORG_LEVELS.filter((level) => this.orgLevels.isShown(level)));
    private readonly orgUnitById = signal<ReadonlyMap<number, OrgAssignmentOrgUnitDto>>(new Map());

    /** Name of the unit at `level` in the record's org unit path. */
    orgLevelName(record: EmployeeOrgAssignmentDto, level: number): string {
        return orgLevelPath(record.organizationUnitId, this.orgUnitById()).get(level)?.displayName ?? '';
    }

    constructor() {
        super();
        this._service.getFieldSettings().subscribe((result) => this.fieldSettings.set(result));
        this._service
            .getOrgUnitsByLevel()
            .subscribe((result) => this.orgUnitById.set(new Map((result.items ?? []).map((u) => [u.id, u] as const))));
        effect(() => {
            if (this.employeeId()) {
                this.reloadPage();
            }
        });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.employeeId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeOrgAssignmentsInput({
                    employeeId: this.employeeId(),
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
        this.createOrEditModal().show(this.employeeId());
    }

    showMenu(event: Event, record: EmployeeOrgAssignmentDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeOrgAssignmentDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeOrgAssignments.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.employeeId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeOrgAssignments.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeOrgAssignmentDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
