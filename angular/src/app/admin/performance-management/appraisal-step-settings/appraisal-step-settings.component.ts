import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AppraisalStepSettingDto,
    AppraisalStepSettingRequestMasterDto,
    AppraisalStepSettingsServiceProxy,
    GetAllAppraisalStepSettingsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { CreateOrEditAppraisalStepSettingModalComponent } from './create-or-edit-appraisal-step-setting-modal.component';

/**
 * "Appraisal WF Setup" (legacy PARequest): per request master and step, what the people on that step of an
 * appraisal's chain may do. Every switch starts off; nothing is seeded, so a tenant sets its steps here
 * before reviewers are added automatically.
 */
@Component({
    templateUrl: './appraisal-step-settings.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditAppraisalStepSettingModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class AppraisalStepSettingsComponent extends AppComponentBase implements OnInit {
    private _service = inject(AppraisalStepSettingsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditAppraisalStepSettingModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly requestMasters = signal<AppraisalStepSettingRequestMasterDto[]>([]);
    readonly requestMasterFilter = signal<number | undefined>(undefined);
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // The table's (onLazyLoad) loads the first page; only the filter's choices are fetched here.
        this._service.getLookups().subscribe((lookups) => this.requestMasters.set(lookups.requestMasters ?? []));
    }

    getRows(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllAppraisalStepSettingsInput({
                    requestMasterId: this.requestMasterFilter(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRows({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    onFilterChange(value: number | undefined): void {
        this.requestMasterFilter.set(value || undefined);
        this.getRows();
    }

    masterName(record: AppraisalStepSettingDto): string {
        return this.isRtl ? record.requestMasterNameAr || record.requestMasterNameEn : record.requestMasterNameEn;
    }

    masterOptionLabel(master: AppraisalStepSettingRequestMasterDto): string {
        const name = this.isRtl ? master.nameAr || master.nameEn : master.nameEn;
        return master.code ? `${master.code} - ${name}` : name;
    }

    create(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: AppraisalStepSettingDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: AppraisalStepSettingDto): MenuItem[] {
        // Every action is listed; the ones this user may not take are disabled rather than hidden.
        return [
            {
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                disabled: !this.permission.isGranted('Pages.PerformanceManagement.AppraisalStepSettings.Edit'),
                command: () => this.createOrEditModal().show(record.id),
            },
            {
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                disabled: !this.permission.isGranted('Pages.PerformanceManagement.AppraisalStepSettings.Delete'),
                command: () => this.delete(record),
            },
        ];
    }

    delete(record: AppraisalStepSettingDto): void {
        this.message.confirm(this.l('AppraisalStepSettingDeleteWarningMessage', this.masterName(record), record.step), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
