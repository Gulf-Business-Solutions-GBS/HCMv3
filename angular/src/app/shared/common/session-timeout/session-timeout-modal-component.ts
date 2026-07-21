import { Component, OnDestroy, inject, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { timer, Subscription } from 'rxjs';
import { AppAuthService } from '../auth/app-auth.service';
import { SessionServiceProxy, UserLoginServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'session-timeout-modal',
    templateUrl: './session-timeout-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe],
})
export class SessionTimeoutModalComponent extends AppComponentBase implements OnDestroy {
    private _appAuthService = inject(AppAuthService);
    private _sessionService = inject(SessionServiceProxy);
    private _userLoginService = inject(UserLoginServiceProxy);

    private subscription: Subscription;

    readonly modal = viewChild<ModalDirective>('modal');

    timeOutSecond = parseInt(this.s('App.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond')); // show inactivity modal when TimeOutSecond passed
    currentSecond: number;
    progresbarWidth = '100%';

    ngOnDestroy(): void {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    start(): void {
        this.currentSecond = this.timeOutSecond;
        this.subscription = timer(0, 1000).subscribe(() => {
            this.changeNotifyContent();
        });
        this.modal().show();
    }

    stop(): void {
        this.currentSecond = this.timeOutSecond;
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
        this.modal().hide();
    }

    private done(): void {
        this.stop();

        let isSessionLockScreenEnabled = abp.setting.getBoolean(
            'App.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut'
        );
        if (isSessionLockScreenEnabled) {
            this.redirectToLockScreen();
        } else {
            this._appAuthService.logout(true);
        }
    }

    private changeNotifyContent(): void {
        this.currentSecond--;
        if (this.currentSecond <= 0) {
            this.progresbarWidth = '0%';
            this.done();
        } else {
            this.progresbarWidth = (this.currentSecond / this.timeOutSecond) * 100 + '%';
        }
    }

    private redirectToLockScreen(): void {
        this._sessionService.getCurrentLoginInformations().subscribe(
            (info) => {
                if (info) {
                    this._userLoginService.getExternalLoginProviderNameByUser(info.user.id).subscribe(
                        (providerName) => {
                            abp.utils.setCookieValue(
                                'userInfo',
                                JSON.stringify({
                                    userName: info.user.userName,
                                    profilePictureId: info.user.profilePictureId,
                                    tenant: info.tenant ? info.tenant.tenancyName : 'Host',
                                    externalLoginProviderName: providerName,
                                }),
                                null,
                                abp.appPath
                            );

                            const path = window.location.pathname;
                            const returnUrl = encodeURI(path);

                            this._appAuthService.logout(true, '/account/session-locked?returnUrl=' + returnUrl);
                        },
                        () => {
                            this._appAuthService.logout(true);
                        }
                    );
                } else {
                    this._appAuthService.logout(true);
                }
            },
            () => {
                this._appAuthService.logout(true);
            }
        );
    }
}
