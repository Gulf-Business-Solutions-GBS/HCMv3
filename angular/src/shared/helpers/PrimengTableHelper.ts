import { signal } from '@angular/core';
import { Paginator } from 'primeng/paginator';
import { Table, TableLazyLoadEvent } from 'primeng/table';
import * as rtlDetect from 'rtl-detect';

export class PrimengTableHelper {
    predefinedRecordsCountPerPage = [5, 10, 25, 50, 100, 250, 500];

    defaultRecordsCountPerPage = 10;

    resizableColumns: false;

    totalRecordsCount = signal(0);

    records = signal<any[]>([]);

    isLoading = signal(false);

    showLoadingIndicator(): void {
        this.isLoading.set(true);
    }

    hideLoadingIndicator(): void {
        this.isLoading.set(false);
    }

    getSorting(table: Table): string {
        let sorting = '';

        if (table.sortMode === 'multiple') {
            if (table.multiSortMeta) {
                for (let i = 0; i < table.multiSortMeta.length; i++) {
                    const element = table.multiSortMeta[i];
                    if (i > 0) {
                        sorting += ',';
                    }
                    sorting += element.field;
                    if (element.order === 1) {
                        sorting += ' ASC';
                    } else if (element.order === -1) {
                        sorting += ' DESC';
                    }
                }
            }
        } else {
            if (table.sortField) {
                sorting = table.sortField;
                if (table.sortOrder === 1) {
                    sorting += ' ASC';
                } else if (table.sortOrder === -1) {
                    sorting += ' DESC';
                }
            }
        }

        return sorting;
    }

    getMaxResultCount(paginator: Paginator, event: TableLazyLoadEvent): number {
        if (paginator.rows) {
            return paginator.rows;
        }

        if (!event) {
            return 0;
        }

        return event.rows;
    }

    getSkipCount(paginator: Paginator, event: TableLazyLoadEvent): number {
        if (paginator.first) {
            return paginator.first;
        }

        if (!event) {
            return 0;
        }

        return event.first;
    }

    shouldResetPaging(event: TableLazyLoadEvent): boolean {
        if (!event /*|| event.sortField*/) {
            // if you want to reset after sorting, comment out parameter
            return true;
        }

        return false;
    }

    adjustScroll(table: Table) {
        const rtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);
        if (!rtl) {
            return;
        }

        const body: HTMLElement = table.el.nativeElement.querySelector('.p-datatable-scrollable-body');
        const header: HTMLElement = table.el.nativeElement.querySelector('.p-datatable-scrollable-header');
        body.addEventListener('scroll', () => {
            header.scrollLeft = body.scrollLeft;
        });
    }
}
