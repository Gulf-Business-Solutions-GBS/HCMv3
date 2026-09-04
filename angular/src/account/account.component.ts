import { Component, OnInit, ViewContainerRef, ViewEncapsulation, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { filter as _filter } from 'lodash-es';
import { LoginService } from './login/login.service';
import { AppPreBootstrap } from 'AppPreBootstrap';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { NgTemplateOutlet } from '@angular/common';
import { TenantChangeComponent } from './shared/tenant-change.component';
import { LanguageSwitchComponent } from './language-switch.component';
import { QrLoginSignalRService } from './qr-login/qr-login-signalr.service';
import { SignalRHelper } from '@shared/helpers/SignalRHelper';
import {
    OAuthService,
    UrlHelperService,
    OAuthLogger,
    OAuthModuleConfig,
    OAuthStorage,
    DateTimeProvider,
} from 'angular-oauth2-oidc';
import { MsalService, MSAL_INSTANCE } from '@azure/msal-angular';
import { IPublicClientApplication, PublicClientApplication } from '@azure/msal-browser';

@Component({
    templateUrl: './account.component.html',
    styleUrls: ['./account.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [NgTemplateOutlet, TenantChangeComponent, RouterOutlet, LanguageSwitchComponent],
    providers: [
        LoginService,
        QrLoginSignalRService,
        OAuthService,
        MsalService,
        UrlHelperService,
        {
            provide: MSAL_INSTANCE,
            useFactory: (): IPublicClientApplication => {
                return new PublicClientApplication({
                    auth: {
                        clientId: 'placeholder',
                        authority: 'https://login.microsoftonline.com/common',
                    },
                    cache: {
                        cacheLocation: 'localStorage',
                    },
                });
            },
        },
        { provide: DateTimeProvider, useFactory: () => ({ now: () => Date.now() }) },
        { provide: OAuthLogger, useFactory: () => console },
        { provide: OAuthModuleConfig, useValue: { resourceServer: { sendAccessToken: false } } },
        { provide: OAuthStorage, useFactory: () => localStorage },
    ],
})
export class AccountComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _loginService = inject(LoginService);
    private _uiCustomizationService = inject(AppUiCustomizationService);
    private _logoService = inject(LogoService);
    private _qrLoginSignalRService = inject(QrLoginSignalRService);

    viewContainerRef: ViewContainerRef;
    tenantLogoUrl: string;

    skin = this.appSession.theme?.baseSettings?.layout.darkMode ? 'dark' : 'light';
    // Built the same way LogoService builds it, so the login page and the sidebar cannot show different
    // logos. Kept inline rather than calling the service because this runs as a field initialiser.
    defaultLogo = AppConsts.appBaseUrl + '/assets/common/images/gbs-logo-on-' + this.skin + '.png';
    backgroundImageName = this.appSession.theme?.baseSettings?.layout.darkMode ? 'login-dark' : 'login';
    readonly qrCodeUrl = signal<string>('');
    qrCodeRefreshInterval: any;

    tenantChangeDisabledRoutes: string[] = [
        'select-edition',
        'gateway-selection',
        'register-tenant',
        'stripe-pre-payment',
        'stripe-post-payment',
        'paypal-pre-payment',
        'paypal-post-payment',
        'stripe-cancel-payment',
        'buy-succeed',
        'extend-succeed',
        'upgrade-succeed',
        'payment-failed',
        'session-locked',
    ];

    public constructor() {
        super();
        const viewContainerRef = inject(ViewContainerRef);
        this.viewContainerRef = viewContainerRef;
        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }

    showTenantChange(): boolean {
        if (!this._router.url) {
            return false;
        }

        if (
            _filter(this.tenantChangeDisabledRoutes, (route) => this._router.url.indexOf('/account/' + route) >= 0)
                .length
        ) {
            return false;
        }

        return abp.multiTenancy.isEnabled && !this.supportsTenancyNameInUrl();
    }

    isSelectEditionPage(): boolean {
        return this._router.url.includes('/account/select-edition');
    }

    isQrLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.IsQrLoginEnabled');
    }

    ngOnInit(): void {
        this._loginService.init();

        if (this.isQrLoginEnabled()) {
            this.initializeQrLoginSignalR();
        }

        document.body.className = this._uiCustomizationService.getAccountModuleBodyClass();
    }

    goToHome(): void {
        (window as any).location.href = '/';
    }

    getBgUrl(): string {
        return 'url(./assets/metronic/themes/' + this.currentTheme.baseSettings.theme + '/images/bg/bg-4.jpg)';
    }

    private supportsTenancyNameInUrl() {
        return AppPreBootstrap.resolveTenancyName(AppConsts.appBaseUrlFormat) != null;
    }

    private initializeQrLoginSignalR() {
        if (this.appSession.application) {
            SignalRHelper.initSignalR(() => {
                this._qrLoginSignalRService.init().then(() => {
                    this.subscribeToEvent('app.qrlogin.generateQrCode', (url) => {
                        this.qrCodeUrl.set(url);
                    });
                });
            });
        }

        this.subscribeToEvent('app.qrlogin.getAuthData', (authData) => {
            this._loginService.qrLoginAuthenticate(
                authData.accessToken,
                authData.encryptedAccessToken,
                authData.expireInSeconds,
                authData.refreshToken,
                authData.refreshTokenExpireInSeconds
            );
        });
    }
}
