import { Component, effect, inject, input, output, viewChild } from '@angular/core';
import { AddMemberModalComponent } from '@app/admin/organization-units/add-member-modal.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitServiceProxy, OrganizationUnitUserListDto } from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { IUserWithOrganizationUnit } from './user-with-organization-unit';
import { IUsersWithOrganizationUnit } from './users-with-organization-unit';
import { finalize } from 'rxjs/operators';

import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AddMemberModalComponent as AddMemberModalComponent_1 } from './add-member-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    selector: 'organization-unit-members',
    templateUrl: './organization-unit-members.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        AddMemberModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class OrganizationUnitMembersComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly organizationUnit = input<IBasicOrganizationUnitInfo>(null);

    readonly memberRemoved = output<IUserWithOrganizationUnit>();
    readonly membersAdded = output<IUsersWithOrganizationUnit>();

    readonly addMemberModal = viewChild<AddMemberModalComponent>('addMemberModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    constructor() {
        super();
        effect(() => {
            const ou = this.organizationUnit();
            if (ou) {
                this.addMemberModal().organizationUnitId.set(ou.id);
                this.refreshMembers();
            }
        });
    }

    getOrganizationUnitUsers(event?: LazyLoadEvent) {
        if (!this.organizationUnit()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();
        this._organizationUnitService
            .getOrganizationUnitUsers(
                this.organizationUnit().id,
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getOrganizationUnitUsers({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    refreshMembers(): void {
        this.reloadPage();
    }

    openAddMemberModal(): void {
        this.addMemberModal().show();
    }

    removeMember(user: OrganizationUnitUserListDto): void {
        this.message.confirm(
            this.l('RemoveUserFromOuWarningMessage', user.userName, this.organizationUnit().displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .removeUserFromOrganizationUnit(user.id, this.organizationUnit().id)
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyRemoved'));
                            this.memberRemoved.emit({
                                userId: user.id,
                                ouId: this.organizationUnit().id,
                            });

                            this.refreshMembers();
                        });
                }
            }
        );
    }

    addMembers(data: any): void {
        this.membersAdded.emit({
            userIds: data.userIds,
            ouId: data.ouId,
        });

        this.refreshMembers();
    }
}
