import { Component, OnInit, inject, viewChild, signal, Injector } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SimpleLookupDto, GetAllSimpleLookupsInput } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SimpleLookupModalComponent } from './simple-lookup-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { ISimpleLookupServiceProxy, SimpleLookupFlag, SimpleLookupRouteData } from './simple-lookup.types';

@Component({
    templateUrl: './simple-lookup.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        SimpleLookupModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class SimpleLookupComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _injector = inject(Injector);

    private _service!: ISimpleLookupServiceProxy;

    readonly createOrEditModal = viewChild<SimpleLookupModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    titleKey = '';
    createPermission = '';
    editPermission = '';
    deletePermission = '';

    flag: SimpleLookupFlag | undefined;
    readonly flaggedIds = signal<ReadonlySet<number>>(new Set());

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    get service(): ISimpleLookupServiceProxy {
        return this._service;
    }

    ngOnInit(): void {
        const data = this._route.snapshot.data as SimpleLookupRouteData;
        this.titleKey = data.titleKey;
        this.createPermission = data.createPermission;
        this.editPermission = data.editPermission;
        this.deletePermission = data.deletePermission;
        this._service = this._injector.get(data.serviceProxyType);
        this.flag = data.flag;
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllSimpleLookupsInput({
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

        this.loadFlaggedIds();
    }

    isFlagged(record: SimpleLookupDto): boolean {
        return this.flaggedIds().has(record.id);
    }

    setFlag(record: SimpleLookupDto, value: boolean): void {
        this.flag.set(this._service, record.id, value).subscribe({
            next: () => {
                const ids = new Set(this.flaggedIds());
                if (value) {
                    ids.add(record.id);
                } else {
                    ids.delete(record.id);
                }
                this.flaggedIds.set(ids);
                this.notify.success(this.l('SavedSuccessfully'));
            },
            error: () => this.loadFlaggedIds(),
        });
    }

    private loadFlaggedIds(): void {
        if (!this.flag) {
            return;
        }

        this.flag.loadIds(this._service).subscribe((result) => this.flaggedIds.set(new Set(result.items ?? [])));
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: SimpleLookupDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: SimpleLookupDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted(this.editPermission)) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted(this.deletePermission)) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: SimpleLookupDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.nameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._service.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
