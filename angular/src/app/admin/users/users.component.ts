import { Component, ViewEncapsulation, AfterViewInit, inject, viewChild, signal, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDtoOfInt64,
    GetUsersInput,
    UserListDto,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { CreateOrEditUserModalComponent } from './create-or-edit-user-modal.component';
import { EditUserPermissionsModalComponent } from './edit-user-permissions-modal.component';
import { ImpersonationService } from './impersonation.service';
import { HttpClient } from '@angular/common/http';
import { FileUpload, FileUploadModule } from 'primeng/fileupload';
import { finalize } from 'rxjs/operators';
import { PermissionTreeModalComponent } from '../shared/permission-tree-modal.component';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { DynamicEntityPropertyManagerComponent } from '@app/shared/common/dynamic-entity-property-manager/dynamic-entity-property-manager.component';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ExcelColumnSelectionModalComponent } from '@app/shared/common/excel-column-selection/excel-column-selection-modal';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { RoleComboComponent } from '../shared/role-combo.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { Tooltip } from 'primeng/tooltip';
import { DynamicEntityPropertyManagerComponent as DynamicEntityPropertyManagerComponent_1 } from '../../shared/common/dynamic-entity-property-manager/dynamic-entity-property-manager.component';
import { ExcelColumnSelectionModalComponent as ExcelColumnSelectionModalComponent_1 } from '../../shared/common/excel-column-selection/excel-column-selection-modal';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { TieredMenu } from 'primeng/tieredmenu';

@Component({
    templateUrl: './users.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./users.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        FileUploadModule,
        FormsModule,
        AutoFocusDirective,
        NgClass,
        PermissionTreeModalComponent,
        RoleComboComponent,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        Tooltip,
        PaginatorModule,
        CreateOrEditUserModalComponent,
        EditUserPermissionsModalComponent,
        DynamicEntityPropertyManagerComponent_1,
        ExcelColumnSelectionModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class UsersComponent extends AppComponentBase implements AfterViewInit {
    private _userServiceProxy = inject(UserServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _activatedRoute = inject(ActivatedRoute);
    private _httpClient = inject(HttpClient);
    private _localStorageService = inject(LocalStorageService);
    private _dateTimeService = inject(DateTimeService);

    _impersonationService = inject(ImpersonationService);

    readonly createOrEditUserModal = viewChild<CreateOrEditUserModalComponent>('createOrEditUserModal');
    readonly editUserPermissionsModal = viewChild<EditUserPermissionsModalComponent>('editUserPermissionsModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly excelFileUpload = viewChild<ElementRef<HTMLInputElement>>('ExcelFileUpload');
    readonly permissionFilterTreeModal = viewChild<PermissionTreeModalComponent>('permissionFilterTreeModal');
    readonly dynamicEntityPropertyManager =
        viewChild<DynamicEntityPropertyManagerComponent>('dynamicEntityPropertyManager');
    readonly excelColumnSelectionModal = viewChild<ExcelColumnSelectionModalComponent>('excelColumnSelectionModal');

    readonly uploadUrl = signal<string>(AppConsts.remoteServiceBaseUrl + '/Users/ImportFromExcel');

    readonly advancedFiltersAreShown = signal<boolean>(false);
    readonly filterText = signal<string>('');
    readonly role = signal<string>('');
    readonly onlyLockedUsers = signal<boolean>(false);
    readonly activeUserMenuItems = signal<MenuItem[]>([]);
    readonly activeExcelMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        this.filterText.set(this._activatedRoute.snapshot.queryParams['filterText'] || '');
    }

    showUserMenu(event: Event, user: UserListDto, menu: any): void {
        this.activeUserMenuItems.set(this.getMenuItemsForUser(user));
        menu.toggle(event);
    }

    getMenuItemsForUser(user: UserListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (
            this.permission.isGranted('Pages.Administration.Users.Impersonation') &&
            user.id !== this.appSession.userId
        ) {
            items.push({
                label: this.l('LoginAsThisUser'),
                icon: 'pi pi-sign-in',
                command: () => this._impersonationService.impersonateUser(user.id, this.appSession.tenantId),
            });
        }

        if (this.permission.isGranted('Pages.Administration.Users.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditUserModal().show(user.id),
            });
        }

        if (this.permission.isGranted('Pages.Administration.Users.ChangePermissions')) {
            items.push({
                label: this.l('Permissions'),
                icon: 'pi pi-lock',
                command: () => this.editUserPermissionsModal().show(user.id, user.userName),
            });
        }

        if (user.lockoutEndDateUtc && this.permission.isGranted('Pages.Administration.Users.Unlock')) {
            items.push({
                label: this.l('Unlock'),
                icon: 'pi pi-unlock',
                command: () => this.unlockUser(user),
            });
        }

        if (this.dynamicEntityPropertyManager().canShow('HCMv3.Authorization.Users.User')) {
            items.push({
                label: this.l('DynamicProperties'),
                icon: 'pi pi-th-large',
                command: () => this.showDynamicProperties(user),
            });
        }

        if (this.permission.isGranted('Pages.Administration.Users.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteUser(user),
            });
        }

        return items;
    }

    showExcelMenu(event: Event, menu: any): void {
        this.activeExcelMenuItems.set(this.getExcelMenuItems());
        menu.toggle(event);
    }

    getExcelMenuItems(): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('ExportToExcel'),
            icon: 'pi pi-download',
            command: () => this.showExcelColumnsSelectionModal(),
        });

        if (this.permission.isGranted('Pages.Administration.Users.Create')) {
            items.push({
                label: this.l('ImportFromExcel'),
                icon: 'pi pi-upload',
                command: () => this.excelFileUpload()?.nativeElement.click(),
            });

            items.push({
                label: this.l(
                    'ImportToExcelSampleFileDownloadInfo',
                    '<a href=/assets/sampleFiles/ImportUsersSampleFile.xlsx download>' + this.l('ClickHere') + '</a>'
                ),
                escape: false,
                styleClass: 'p-menuitem-text text-muted small',
            });
        }

        return items;
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            const file = input.files[0];
            this.uploadExcel({ files: file });
            input.value = ''; // Reset input for re-upload
        }
    }

    ngAfterViewInit(): void {
        this.primengTableHelper.adjustScroll(this.dataTable());
    }

    getUsers(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._userServiceProxy
            .getUsers(
                new GetUsersInput({
                    filter: this.filterText(),
                    permissions: this.permissionFilterTreeModal().getSelectedPermissions(),
                    role: this.role() !== '' ? parseInt(this.role()) : undefined,
                    onlyLockedUsers: this.onlyLockedUsers(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.setUsersProfilePictureUrl(this.primengTableHelper.records());
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    unlockUser(record): void {
        this._userServiceProxy.unlockUser(new EntityDtoOfInt64({ id: record.id })).subscribe(() => {
            this.notify.success(this.l('UnlockedTheUser', record.userName));
            this.reloadPage();
        });
    }

    getRolesAsString(roles): string {
        let roleNames = '';

        for (let j = 0; j < roles.length; j++) {
            if (roleNames.length) {
                roleNames = roleNames + ', ';
            }

            roleNames = roleNames + roles[j].roleName;
        }

        return roleNames;
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    exportToExcel($event): void {
        this._userServiceProxy
            .getUsersToExcel(
                this.filterText(),
                this.permissionFilterTreeModal().getSelectedPermissions(),
                $event,
                this.role() !== '' ? parseInt(this.role()) : undefined,
                this.onlyLockedUsers(),
                this.primengTableHelper.getSorting(this.dataTable())
            )
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    createUser(): void {
        this.createOrEditUserModal().show();
    }

    uploadExcel(data: { files: File }): void {
        const formData: FormData = new FormData();
        const file = data.files;
        formData.append('file', file, file.name);

        this._httpClient.post<any>(this.uploadUrl(), formData).subscribe((response: any) => {
            if (response?.success) {
                this.notify.success(this.l('ImportUsersProcessStart'));
            } else if (response?.error != null) {
                this.notify.error(this.l('ImportUsersUploadFailed'));
            }
        });
    }

    onUploadExcelError(): void {
        this.notify.error(this.l('ImportUsersUploadFailed'));
    }

    deleteUser(user: UserListDto): void {
        if (user.userName === AppConsts.userManagement.defaultAdminUserName) {
            this.message.warn(this.l('{0}UserCannotBeDeleted', AppConsts.userManagement.defaultAdminUserName));
            return;
        }

        this.message.confirm(this.l('UserDeleteWarningMessage', user.userName), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._userServiceProxy.deleteUser(user.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    showDynamicProperties(user: UserListDto): void {
        this.dynamicEntityPropertyManager()
            .getModal()
            .show('HCMv3.Authorization.Users.User', user.id.toString());
    }

    setUsersProfilePictureUrl(users: UserListDto[]): void {
        for (let i = 0; i < users.length; i++) {
            let user = users[i];
            this._localStorageService.getItem(AppConsts.authorization.encrptedAuthTokenName, function (err, value) {
                let profilePictureUrl =
                    AppConsts.remoteServiceBaseUrl +
                    '/Profile/GetProfilePictureByUser?userId=' +
                    user.id +
                    '&' +
                    AppConsts.authorization.encrptedAuthTokenName +
                    '=' +
                    encodeURIComponent(value.token);
                (user as any).profilePictureUrl = profilePictureUrl;
            });
        }
    }

    isUserLocked(user: UserListDto): boolean {
        if (!user.lockoutEndDateUtc) {
            return false;
        }

        let lockoutEndDateUtc = this._dateTimeService.changeDateTimeZone(user.lockoutEndDateUtc, 'UTC');
        return lockoutEndDateUtc > this._dateTimeService.getUTCDate().toDateTime();
    }

    showExcelColumnsSelectionModal(): void {
        this._userServiceProxy.getUserExcelColumnsToExcel().subscribe((result) => {
            this.excelColumnSelectionModal().show(result);
        });
    }
}
