import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    BulkPasswordCandidateDto,
    BulkPasswordServiceProxy,
    BulkResetPasswordsInput,
    BulkResetPasswordsResultDto,
    GetBulkPasswordCandidatesInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of legacy HCM10.Web/UserControls/Administration/uscRandomPassword.ascx.
 *
 * Legacy fired SetAllUsersPassword straight off a button. This port requires an explicit confirmation that
 * names how many accounts are about to be reset and which ones, because the reset locks those users out
 * until they read the generated password e-mail.
 */
@Component({
    templateUrl: './bulk-password-reset.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class BulkPasswordResetComponent extends AppComponentBase implements OnInit {
    private _bulkPasswordService = inject(BulkPasswordServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly saving = signal<boolean>(false);
    readonly result = signal<BulkResetPasswordsResultDto | undefined>(undefined);
    readonly errorMessage = signal<string>('');

    /** Employee id -> candidate, so a selection survives paging. */
    readonly selected = signal<Map<number, BulkPasswordCandidateDto>>(new Map());

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getCandidates().
    }

    getCandidates(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._bulkPasswordService
            .getBulkPasswordCandidates(
                new GetBulkPasswordCandidatesInput({
                    filter: this.filterText(),
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

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getCandidates({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    isSelected(record: BulkPasswordCandidateDto): boolean {
        return this.selected().has(record.id);
    }

    toggleSelection(record: BulkPasswordCandidateDto, checked: boolean): void {
        const next = new Map(this.selected());

        if (checked) {
            next.set(record.id, record);
        } else {
            next.delete(record.id);
        }

        this.selected.set(next);
    }

    isPageFullySelected(): boolean {
        const page = this.primengTableHelper.records() as BulkPasswordCandidateDto[];
        return page.length > 0 && page.every((r) => this.selected().has(r.id));
    }

    togglePageSelection(checked: boolean): void {
        const next = new Map(this.selected());

        for (const record of this.primengTableHelper.records() as BulkPasswordCandidateDto[]) {
            if (checked) {
                next.set(record.id, record);
            } else {
                next.delete(record.id);
            }
        }

        this.selected.set(next);
    }

    clearSelection(): void {
        this.selected.set(new Map());
    }

    selectedCount(): number {
        return this.selected().size;
    }

    resetPasswords(): void {
        const selection = Array.from(this.selected().values());

        if (selection.length === 0) {
            this.message.warn(this.l('NoUsersSelected'));
            return;
        }

        // Name the accounts explicitly -- these users are locked out the moment this runs.
        const preview = selection
            .slice(0, 20)
            .map((c) => `${c.code} - ${c.employeeName} (${c.userName}) -> ${c.email}`)
            .join('\n');
        const listing = selection.length > 20 ? `${preview}\n...` : preview;

        this.message.confirm(
            this.l('ConfirmBulkResetPasswords', selection.length, listing),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.saving.set(true);
                this.result.set(undefined);
                this.errorMessage.set('');

                this._bulkPasswordService
                    .bulkResetPasswords(new BulkResetPasswordsInput({ employeeIds: selection.map((c) => c.id) }))
                    .pipe(finalize(() => this.saving.set(false)))
                    .subscribe({
                        next: (r) => {
                            this.result.set(r);
                            this.clearSelection();
                            this.notify.success(
                                this.l(
                                    'BulkResetPasswordsResultSummary',
                                    r.updatedPasswordCount,
                                    r.emailedPasswordCount
                                )
                            );
                        },
                        error: (e) => this.errorMessage.set(e?.message || e?.details || ''),
                    });
            }
        );
    }
}
