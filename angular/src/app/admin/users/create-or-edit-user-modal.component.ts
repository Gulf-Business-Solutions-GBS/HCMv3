import {
    Component,
    ViewEncapsulation,
    inject,
    output,
    signal,
    viewChild,
    afterNextRender,
    ElementRef,
    Injector,
    runInInjectionContext,
} from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrUpdateUserInput,
    OrganizationUnitDto,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    UserEditDto,
    UserRoleDto,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import {
    IOrganizationUnitsTreeComponentData,
    OrganizationUnitsTreeComponent,
} from '../shared/organization-unit-tree.component';
import { map as _map, filter as _filter } from 'lodash-es';
import { finalize, switchMap, tap } from 'rxjs/operators';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective, TabHeadingDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { Tooltip } from 'primeng/tooltip';
import { EqualValidator } from '../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../shared/utils/validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { ChangeProfilePictureModalComponent } from '../../shared/layout/profile/change-profile-picture-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditUserModal',
    templateUrl: './create-or-edit-user-modal.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['create-or-edit-user-modal.component.less'],
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        Tooltip,
        EqualValidator,
        PasswordComplexityValidator,
        TabHeadingDirective,
        OrganizationUnitsTreeComponent,
        ButtonBusyDirective,
        ChangeProfilePictureModalComponent,
        LocalizePipe,
    ],
})
export class CreateOrEditUserModalComponent extends AppComponentBase {
    private _userService = inject(UserServiceProxy);
    private _profileService = inject(ProfileServiceProxy);
    private _injector = inject(Injector);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly organizationUnitTree = viewChild<OrganizationUnitsTreeComponent>('organizationUnitTree');
    readonly nameInput = viewChild<ElementRef>('nameInput');

    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly canChangeUserName = signal<boolean>(true);
    readonly canChangeProfilePicture = signal<boolean>(false);
    readonly isTwoFactorEnabled = signal<boolean>(
        this.setting.getBoolean('Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled')
    );
    readonly isLockoutEnabled = signal<boolean>(
        this.setting.getBoolean('Abp.Zero.UserManagement.UserLockOut.IsEnabled')
    );
    readonly passwordComplexitySetting = signal<PasswordComplexitySetting>(new PasswordComplexitySetting());
    readonly user = signal<UserEditDto>(new UserEditDto());
    readonly roles = signal<UserRoleDto[]>([]);
    readonly sendActivationEmail = signal<boolean>(true);
    readonly setRandomPassword = signal<boolean>(true);
    readonly passwordComplexityInfo = signal<string>('');
    readonly profilePicture = signal<string>('');
    readonly allowedUserNameCharacters = signal<string>('');
    readonly isSMTPSettingsProvided = signal<boolean>(false);
    readonly passwordMeterInitialized = signal<boolean>(false);
    readonly allOrganizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly memberedOrganizationUnits = signal<string[]>([]);
    readonly organizationUnitData = signal<IOrganizationUnitsTreeComponentData>(undefined);
    readonly isEditMode = signal<boolean>(false);
    readonly userPasswordRepeat = signal<string>('');

    show(userId?: number): void {
        this.isEditMode.set(userId != null);
        if (userId == null) {
            this.active.set(true);
            this.setRandomPassword.set(true);
            this.sendActivationEmail.set(true);
            this.canChangeProfilePicture.set(false);
        } else {
            this.canChangeProfilePicture.set(
                this.permission.isGranted('Pages.Administration.Users.ChangeProfilePicture')
            );
        }

        this._userService
            .getUserForEdit(userId)
            .pipe(
                tap((userResult) => {
                    this.user.set(userResult.user);
                    this.roles.set(userResult.roles);
                    this.canChangeUserName.set(
                        userResult.user.userName !== AppConsts.userManagement.defaultAdminUserName
                    );
                    this.allowedUserNameCharacters.set(userResult.allowedUserNameCharacters);
                    this.isSMTPSettingsProvided.set(userResult.isSMTPSettingsProvided);
                    this.sendActivationEmail.set(userResult.isSMTPSettingsProvided);
                    this.allOrganizationUnits.set(userResult.allOrganizationUnits);
                    this.memberedOrganizationUnits.set(userResult.memberedOrganizationUnits);
                    this.getProfilePicture(userId);

                    if (userId) {
                        this.active.set(true);
                        this.setRandomPassword.set(false);
                        this.setRandomPasswordChange(undefined);
                        this.sendActivationEmail.set(false);
                    }
                }),
                switchMap(() => this._profileService.getPasswordComplexitySetting())
            )
            .subscribe((passwordComplexityResult) => {
                this.passwordComplexitySetting.set(passwordComplexityResult.setting);
                this.setPasswordComplexityInfo();
                this.modal().show();
            });
    }

    setPasswordComplexityInfo(): void {
        let info = '<ul>';
        const settings = this.passwordComplexitySetting();

        if (settings.requireDigit) {
            info += '<li>' + this.l('PasswordComplexity_RequireDigit_Hint') + '</li>';
        }

        if (settings.requireLowercase) {
            info += '<li>' + this.l('PasswordComplexity_RequireLowercase_Hint') + '</li>';
        }

        if (settings.requireUppercase) {
            info += '<li>' + this.l('PasswordComplexity_RequireUppercase_Hint') + '</li>';
        }

        if (settings.requireNonAlphanumeric) {
            info += '<li>' + this.l('PasswordComplexity_RequireNonAlphanumeric_Hint') + '</li>';
        }

        if (settings.requiredLength) {
            info += '<li>' + this.l('PasswordComplexity_RequiredLength_Hint', settings.requiredLength) + '</li>';
        }

        info += '</ul>';
        this.passwordComplexityInfo.set(info);
    }

    getProfilePicture(userId: number): void {
        if (!userId) {
            this.profilePicture.set(this.appRootUrl() + 'assets/common/images/default-profile-picture.png');
            return;
        }

        this._profileService.getProfilePictureByUser(userId).subscribe((result) => {
            if (result && result.profilePicture) {
                this.profilePicture.set('data:image/jpeg;base64,' + result.profilePicture);
            } else {
                this.profilePicture.set(this.appRootUrl() + 'assets/common/images/default-profile-picture.png');
            }
        });
    }

    onShown(): void {
        this.organizationUnitData.set({
            allOrganizationUnits: this.allOrganizationUnits(),
            selectedOrganizationUnits: this.memberedOrganizationUnits(),
        });

        const input = this.nameInput();
        if (input?.nativeElement) {
            input.nativeElement.focus();
        }
    }

    save(): void {
        let input = new CreateOrUpdateUserInput();

        input.user = this.user();
        input.setRandomPassword = this.setRandomPassword();
        input.sendActivationEmail = this.sendActivationEmail();
        input.assignedRoleNames = _map(
            _filter(this.roles(), { isAssigned: true, inheritedFromOrganizationUnit: false }),
            (role) => role.roleName
        );

        input.organizationUnits = this.organizationUnitTree().getSelectedOrganizationIds();

        this.saving.set(true);
        this._userService
            .createOrUpdateUser(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.userPasswordRepeat.set('');
        this.modal().hide();
    }

    getAssignedRoleCount(): number {
        return _filter(this.roles(), { isAssigned: true }).length;
    }

    getAssignedMemberedOrganizationUnitCount(): number {
        const units = this.memberedOrganizationUnits();
        return units ? units.length : 0;
    }

    onOrganizationUnitTreeSelectionChanged(): void {
        let organizationUnits = this.organizationUnitTree().getSelectedOrganizations();
        this.memberedOrganizationUnits.set(_map(organizationUnits, (ou) => ou.code));
    }

    setRandomPasswordChange(event): void {
        if (this.passwordMeterInitialized() && this.setRandomPassword()) {
            this.passwordMeterInitialized.set(false);
            return;
        }

        runInInjectionContext(this._injector, () => {
            afterNextRender(() => {
                PasswordMeterComponent.bootstrap();
                this.passwordMeterInitialized.set(true);
            });
        });
    }

    updateUserName(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.name = value;
            return updated;
        });
    }

    updateUserSurname(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.surname = value;
            return updated;
        });
    }

    updateUserEmail(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.emailAddress = value;
            return updated;
        });
    }

    updateUserPhone(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.phoneNumber = value;
            return updated;
        });
    }

    updateUserUserName(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.userName = value;
            return updated;
        });
    }

    updateUserPassword(value: string): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.password = value;
            return updated;
        });
    }

    updateUserShouldChangePassword(value: boolean): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.shouldChangePasswordOnNextLogin = value;
            return updated;
        });
    }

    updateUserIsActive(value: boolean): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.isActive = value;
            return updated;
        });
    }

    updateUserIsTwoFactorEnabled(value: boolean): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.isTwoFactorEnabled = value;
            return updated;
        });
    }

    updateUserIsLockoutEnabled(value: boolean): void {
        this.user.update((current) => {
            const updated = new UserEditDto();
            Object.assign(updated, current);
            updated.isLockoutEnabled = value;
            return updated;
        });
    }

    updateRoleAssignment(index: number, value: boolean): void {
        this.roles.update((currentRoles) => {
            const updatedRoles = currentRoles.map((role, i) => {
                if (i === index) {
                    const updated = new UserRoleDto();
                    Object.assign(updated, role);
                    updated.isAssigned = value;
                    return updated;
                }
                return role;
            });
            return updatedRoles;
        });
    }
}
