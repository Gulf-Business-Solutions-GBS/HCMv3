import { Component, output, viewChild } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NameValueDto, PagedResultDtoOfFindUsersOutputDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { merge as _merge } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
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
    canSelect?: (item: NameValueDto) => boolean | Observable<boolean>;
    loadOnStartup?: boolean;
    pageSize?: number;
}

//For more modal options http://valor-software.com/ngx-bootstrap/#/modals#modal-directive

@Component({
    selector: 'commonLookupModal',
    templateUrl: './common-lookup-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        AutoFocusDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        LocalizePipe,
    ],
})
export class CommonLookupModalComponent extends AppComponentBase {
    static defaultOptions: ICommonLookupModalOptions = {
        dataSource: undefined,
        canSelect: () => true,
        loadOnStartup: true,
        isFilterEnabled: true,
        pageSize: AppConsts.grid.defaultPageSize,
    };

    readonly itemSelected = output<NameValueDto>();

    readonly modal = viewChild<ModalDirective>('modal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    options: ICommonLookupModalOptions = _merge({});

    isShown = false;
    isInitialized = false;
    filterText = '';
    excludeCurrentUser = true;
    tenantId?: number;

    configure(options: ICommonLookupModalOptions): void {
        this.options = _merge(
            {},
            CommonLookupModalComponent.defaultOptions,
            { title: this.l('SelectAnItem') },
            options
        );
    }

    show(): void {
        if (!this.options) {
            throw Error(
                'Should call CommonLookupModalComponent.configure once before CommonLookupModalComponent.show!'
            );
        }

        this.modal().show();
    }

    refreshTable(): void {
        this.paginator().changePage(this.paginator().getPage());
        this.shown();
    }

    close(): void {
        this.modal().hide();
    }

    shown(): void {
        this.isShown = true;
        this.getRecordsIfNeeds(null);
    }

    getRecordsIfNeeds(event?: LazyLoadEvent): void {
        if (!this.isShown) {
            return;
        }

        if (!this.options.loadOnStartup && !this.isInitialized) {
            return;
        }

        this.getRecords(event);
        this.isInitialized = true;
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
            .dataSource(skipCount, maxResultCount, this.filterText, this.tenantId, this.excludeCurrentUser)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    selectItem(item: NameValueDto) {
        const boolOrPromise = this.options.canSelect(item);
        if (!boolOrPromise) {
            return;
        }

        if (boolOrPromise === true) {
            this.itemSelected.emit(item);
            this.close();
            return;
        }

        //assume as observable
        (boolOrPromise as Observable<boolean>).subscribe((result) => {
            if (result) {
                this.itemSelected.emit(item);
                this.close();
            }
        });
    }
}
