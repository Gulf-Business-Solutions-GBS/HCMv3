import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllOrganizationDocumentsInput,
    OrganizationDocumentDto,
    OrgPoliciesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

/**
 * Legacy "Org Policy" (OrganizationDocument_Listview_ShowAll): the organisation documents whose type is
 * marked Show To All, for everyone to read. No create, edit or delete; a row opens to show its files.
 */
@Component({
    templateUrl: './org-policies.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        LuxonFormatPipe,
        DecimalPipe,
        AttachmentsComponent,
    ],
})
export class OrgPoliciesComponent extends AppComponentBase implements OnInit {
    private _service = inject(OrgPoliciesServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly openId = signal<number | undefined>(undefined);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getPolicies().
    }

    getPolicies(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllOrganizationDocumentsInput({
                    filter: this.filterText(),
                    organizationUnitId: undefined,
                    sponsorId: undefined,
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

    toggleFiles(record: OrganizationDocumentDto): void {
        this.openId.set(this.openId() === record.id ? undefined : record.id);
    }
}
