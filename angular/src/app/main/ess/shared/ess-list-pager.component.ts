import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EssList } from './ess-list';

/** The pager under every self-service request list: the application's usual one, driven by the shared list. */
@Component({
    selector: 'ess-list-pager',
    imports: [PaginatorModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="primeng-paging-container">
            <p-paginator
                [rows]="list().pageSize()"
                [first]="list().page() * list().pageSize()"
                [totalRecords]="list().total()"
                [rowsPerPageOptions]="list().pageSizes"
                [showCurrentPageReport]="true"
                [currentPageReportTemplate]="'TotalRecordsCount' | localize: list().total()"
                (onPageChange)="onPage($event)" />
        </div>
    `,
})
export class EssListPagerComponent {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    readonly list = input.required<EssList<any>>();

    onPage(event: PaginatorState): void {
        this.list().pageSize.set(event.rows ?? this.list().pageSize());
        this.list().page.set(event.page ?? 0);
    }
}
