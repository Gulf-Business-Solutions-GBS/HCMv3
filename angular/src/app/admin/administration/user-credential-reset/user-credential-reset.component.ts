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
    CredentialResetRoleDto,
    CredentialResetUserDto,
    GetCredentialResetUsersInput,
    ResetUserCredentialsInput,
    ResetUserCredentialsResultDto,
    UserCredentialResetServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Reset User Passwords (Administration > Tools). Legacy uscRandomPassword + the "by role" half of
 * uscChangePassword, on ASP.NET Zero: Zero's password rules, Zero's credentials e-mail, and "change password
 * at next sign-in" on every reset. Pick users from the list, or pick a role and reset everyone in it.
 *
 * Every reset is confirmed first, naming how many accounts it touches -- those users' old passwords stop
 * working the moment it runs.
 */
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

@Component({
    templateUrl: './user-credential-reset.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class UserCredentialResetComponent extends AppComponentBase implements OnInit {
    private _service = inject(UserCredentialResetServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly roleId = signal<number | undefined>(undefined);
    readonly roles = signal<CredentialResetRoleDto[]>([]);
    readonly saving = signal<boolean>(false);
    readonly result = signal<ResetUserCredentialsResultDto | undefined>(undefined);

    /** User id -> row, so a selection survives paging and filtering. */
    readonly selected = signal<Map<number, CredentialResetUserDto>>(new Map());

    ngOnInit(): void {
        // The PrimeNG table's (onLazyLoad) loads the first page; only the roles are fetched here.
        this._service.getRoles().subscribe((r) => this.roles.set(r.items));
    }

    getUsers(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getUsers(
                new GetCredentialResetUsersInput({
                    filter: this.filterText(),
                    roleId: this.roleId(),
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

    /** A new filter or role starts again from the first page. */
    reloadPage(): void {
        if (this.paginator().getPage() !== 0) {
            this.paginator().changePage(0);
            return;
        }

        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getUsers({ first: 0, rows } as LazyLoadEvent);
    }

    onRoleChange(roleId: number | undefined): void {
        this.roleId.set(roleId);
        this.reloadPage();
    }

    selectedRoleName(): string {
        return this.roles().find((r) => r.id === this.roleId())?.displayName ?? '';
    }

    isSelected(record: CredentialResetUserDto): boolean {
        return this.selected().has(record.id);
    }

    toggleSelection(record: CredentialResetUserDto, checked: boolean): void {
        const next = new Map(this.selected());
        if (checked) {
            next.set(record.id, record);
        } else {
            next.delete(record.id);
        }
        this.selected.set(next);
    }

    /** Rows without an e-mail address cannot be reset, so they are never part of a page selection. */
    private selectablePage(): CredentialResetUserDto[] {
        return (this.primengTableHelper.records() as CredentialResetUserDto[]).filter((r) => r.hasEmailAddress);
    }

    isPageFullySelected(): boolean {
        const page = this.selectablePage();
        return page.length > 0 && page.every((r) => this.selected().has(r.id));
    }

    togglePageSelection(checked: boolean): void {
        const next = new Map(this.selected());
        for (const record of this.selectablePage()) {
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

    resetSelected(): void {
        const selection = Array.from(this.selected().values());
        if (selection.length === 0) {
            this.message.warn(this.l('NoUsersSelected'));
            return;
        }

        const preview = selection
            .slice(0, 20)
            .map((u) => `${u.userName} - ${u.fullName} -> ${u.emailAddress}`)
            .join('\n');
        const listing = selection.length > 20 ? `${preview}\n...` : preview;

        this.message.confirm(
            this.l('CredentialResetConfirm', selection.length, listing),
            this.l('AreYouSure'),
            (ok) => {
                if (ok) {
                    this.run(new ResetUserCredentialsInput({ userIds: selection.map((u) => u.id), roleId: undefined }));
                }
            }
        );
    }

    resetRole(): void {
        const roleId = this.roleId();
        if (!roleId) {
            return;
        }

        this.message.confirm(
            this.l('CredentialResetRoleConfirm', this.selectedRoleName()),
            this.l('AreYouSure'),
            (ok) => {
                if (ok) {
                    this.run(new ResetUserCredentialsInput({ userIds: [], roleId }));
                }
            }
        );
    }

    private run(input: ResetUserCredentialsInput): void {
        this.saving.set(true);
        this.result.set(undefined);

        this._service
            .resetCredentials(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((r) => {
                this.result.set(r);
                this.clearSelection();
                this.notify.success(
                    this.l('BulkResetPasswordsResultSummary', r.passwordsResetCount, r.emailsSentCount)
                );
            });
    }
}
