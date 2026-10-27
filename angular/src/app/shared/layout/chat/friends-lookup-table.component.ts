import { Component, OnInit, inject, output, signal, viewChild } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    FindUsersInput,
    FindUsersOutputDto,
    PagedResultDtoOfFindUsersOutputDto,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';

import { AutoFocusDirective } from '../../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

export interface ICommonLookupModalOptions {
    title?: string;
    isFilterEnabled?: boolean;
    dataSource: (
        skipCount: number,
        maxResultCount: number,
        filter: string,
        tenantId?: number,
        excludeCurrentUser?: boolean
    ) => Observable<PagedResultDtoOfFindUsersOutputDto>;
    canSelect?: (item: FindUsersOutputDto) => boolean | Observable<boolean>;
    loadOnStartup?: boolean;
    pageSize?: number;
}

@Component({
    selector: 'friendsLookupTable',
    templateUrl: './friends-lookup-table.component.html',
    imports: [FormsModule, AutoFocusDirective, BusyIfDirective, TableModule, PaginatorModule, LocalizePipe],
})
export class FriendsLookupTableComponent extends AppComponentBase implements OnInit {
    private _commonLookupService = inject(CommonLookupServiceProxy);

    readonly itemSelected = output<FindUsersOutputDto>();

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly saving = signal(false);
    readonly isInitialized = signal(false);
    readonly filterText = signal('');
    readonly excludeCurrentUser = signal(true);
    readonly tenantId = signal<number | undefined>(undefined);

    options: ICommonLookupModalOptions = {
        dataSource: (
            skipCount: number,
            maxResultCount: number,
            filter: string,
            tenantId?: number,
            excludeCurrentUser?: boolean
        ) => {
            const input = new FindUsersInput();
            input.filter = filter;
            input.maxResultCount = maxResultCount;
            input.skipCount = skipCount;
            input.tenantId = tenantId;
            input.excludeCurrentUser = excludeCurrentUser;
            return this._commonLookupService.findUsers(input);
        },
        title: this.l('AddFriend'),
        canSelect: () => true,
        loadOnStartup: true,
        isFilterEnabled: true,
        pageSize: AppConsts.grid.defaultPageSize,
    };

    ngOnInit(): void {}

    refreshTable(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    getRecordsIfNeeds(event?: LazyLoadEvent): void {
        if (!this.options.loadOnStartup && !this.isInitialized()) {
            return;
        }

        this.getRecords(event);
        this.isInitialized.set(true);
    }

    getRecords(event?: LazyLoadEvent): void {
        const maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);
        const skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this.options
            .dataSource(skipCount, maxResultCount, this.filterText(), this.tenantId(), this.excludeCurrentUser())
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    selectItem(item: FindUsersOutputDto) {
        const boolOrPromise = this.options.canSelect(item);
        if (!boolOrPromise) {
            return;
        }

        if (boolOrPromise === true) {
            this.itemSelected.emit(item);
            return;
        }

        //assume as observable
        (boolOrPromise as Observable<boolean>).subscribe((result) => {
            if (result) {
                this.itemSelected.emit(item);
            }
        });
    }
}
