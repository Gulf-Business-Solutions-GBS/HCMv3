import { Component, OnInit, inject, input, signal } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { LinkedUserDto, ProfileServiceProxy, UserLinkServiceProxy } from '@shared/service-proxies/service-proxies';
import { LinkedAccountService } from '@app/shared/layout/linked-account.service';
import { AbpMultiTenancyService, AbpSessionService } from 'abp-ng2-module';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { ImpersonationService } from '@app/admin/users/impersonation.service';
import { AppConsts } from '@shared/AppConsts';
import { Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    selector: 'user-menu',
    templateUrl: './user-menu.component.html',
    imports: [NgClass, RouterLink, LocalizePipe, PermissionPipe],
})
export class UserMenuComponent extends ThemesLayoutBaseComponent implements OnInit {
    private _router = inject(Router);
    private _linkedAccountService = inject(LinkedAccountService);
    private _abpMultiTenancyService = inject(AbpMultiTenancyService);
    private _profileServiceProxy = inject(ProfileServiceProxy);
    private _userLinkServiceProxy = inject(UserLinkServiceProxy);
    private _authService = inject(AppAuthService);
    private _impersonationService = inject(ImpersonationService);
    private _abpSessionService = inject(AbpSessionService);

    readonly iconOnly = input(false);

    readonly togglerCssClass = input('cursor-pointer symbol symbol-35px symbol-md-40px');
    readonly profileImageCssClass = input('');
    //TODO@Metronic8 -> we may delete this.
    readonly textCssClass = input('text-dark-50 fw-bolder fs-base d-none d-md-inline me-3');
    readonly symbolCssClass = input('symbol symbol-lg-30px symbol-20px');
    readonly symbolTextCssClass = input('symbol-label fs-2 fw-bold bg-success text-inverse-success');

    readonly usernameFirstLetter = signal<string>('');

    readonly profilePicture = signal<string>(
        AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png'
    );
    readonly shownLoginName = signal<string>('');
    readonly tenancyName = signal<string>('');
    readonly userName = signal<string>('');
    readonly emailAddress = signal<string>('');

    readonly recentlyLinkedUsers = signal<LinkedUserDto[]>([]);
    readonly isImpersonatedLogin = signal<boolean>(false);
    readonly isMultiTenancyEnabled = signal<boolean>(false);

    mQuickUserOffcanvas: any;

    ngOnInit(): void {
        this.isImpersonatedLogin.set(this._abpSessionService.impersonatorUserId > 0);
        this.isMultiTenancyEnabled.set(this._abpMultiTenancyService.isEnabled);
        this.setCurrentLoginInformations();
        this.getProfilePicture();
        this.getRecentlyLinkedUsers();
        this.registerToEvents();
        this.usernameFirstLetter.set(this.appSession.user.userName.substring(0, 1).toUpperCase());
    }

    setCurrentLoginInformations(): void {
        this.shownLoginName.set(this.appSession.getShownLoginName());
        // The host is not a tenant and has no tenancy name, which left a bare "\admin" in the menu.
        this.tenancyName.set(this.appSession.tenancyName || 'Host');
        this.userName.set(this.appSession.user.userName);
        this.emailAddress.set(this.appSession.user.emailAddress);
    }

    getShownUserName(linkedUser: LinkedUserDto): string {
        if (!this._abpMultiTenancyService.isEnabled) {
            return linkedUser.username;
        }

        return (linkedUser.tenantId ? linkedUser.tenancyName : '.') + '\\' + linkedUser.username;
    }

    getProfilePicture(): void {
        this._profileServiceProxy.getProfilePicture().subscribe((result) => {
            if (result && result.profilePicture) {
                this.profilePicture.set('data:image/jpeg;base64,' + result.profilePicture);
            }
        });
    }

    getRecentlyLinkedUsers(): void {
        this._userLinkServiceProxy.getRecentlyUsedLinkedUsers().subscribe((result) => {
            this.recentlyLinkedUsers.set(result.items);
        });
    }

    showLoginAttempts(): void {
        this._router.navigate(['/app/admin/login-attempts']);
    }

    showLinkedAccounts(): void {
        abp.event.trigger('app.show.linkedAccountsModal');
    }

    showUserDelegations(): void {
        abp.event.trigger('app.show.userDelegationsModal');
    }

    changePassword(): void {
        abp.event.trigger('app.show.changePasswordModal');
    }

    changeProfilePicture(): void {
        abp.event.trigger('app.show.changeProfilePictureModal');
    }

    changeMySettings(): void {
        abp.event.trigger('app.show.mySettingsModal');
    }

    registerToEvents() {
        this.subscribeToEvent('profilePictureChanged', () => {
            this.getProfilePicture();
        });

        this.subscribeToEvent('app.getRecentlyLinkedUsers', () => {
            this.getRecentlyLinkedUsers();
        });

        this.subscribeToEvent('app.onMySettingsModalSaved', () => {
            this.onMySettingsModalSaved();
        });
    }

    logout(): void {
        this._authService.logout();
    }

    onMySettingsModalSaved(): void {
        this.shownLoginName.set(this.appSession.getShownLoginName());
    }

    backToMyAccount(): void {
        this._impersonationService.backToImpersonator();
    }

    switchToLinkedUser(linkedUser: LinkedUserDto): void {
        this._linkedAccountService.switchToAccount(linkedUser.id, linkedUser.tenantId);
    }

    downloadCollectedData(): void {
        this._profileServiceProxy.prepareCollectedData().subscribe(() => {
            this.message.success(this.l('GdprDataPrepareStartedNotification'));
        });
    }

    isMobileDevice(): any {
        return KTUtil.isMobileDevice();
    }
}
