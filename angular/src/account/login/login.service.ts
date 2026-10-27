import { TokenService, LogService, MessageService, LocalizationService } from 'abp-ng2-module';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AppConsts } from '@shared/AppConsts';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import {
    AuthenticateModel,
    AuthenticateResultModel,
    ExternalAuthenticateModel,
    ExternalAuthenticateResultModel,
    ExternalLoginProviderInfoModel,
    PasswordlessAuthenticateModel,
    PasswordlessAuthenticateResultModel,
    TokenAuthServiceProxy,
    TwitterServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { map as _map, filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { NgxSpinnerService } from 'ngx-spinner';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { AuthenticationResult, PublicClientApplication } from '@azure/msal-browser';
import { MsalService } from '@azure/msal-angular';

declare const FB: any; // Facebook API
declare const gapi: any; // google API
declare const google: any; // google

export class ExternalLoginProvider extends ExternalLoginProviderInfoModel {
    static readonly FACEBOOK: string = 'Facebook';
    static readonly GOOGLE: string = 'Google';
    static readonly MICROSOFT: string = 'Microsoft';
    static readonly OPENID: string = 'OpenIdConnect';
    static readonly WSFEDERATION: string = 'WsFederation';
    static readonly TWITTER: string = 'Twitter';

    icon: string;
    initialized = false;

    constructor(providerInfo: ExternalLoginProviderInfoModel) {
        super();

        this.name = providerInfo.name;
        this.clientId = providerInfo.clientId;
        this.additionalParams = providerInfo.additionalParams;
        this.icon = providerInfo.name.toLowerCase();
    }
}

@Injectable()
export class LoginService {
    private _tokenAuthService = inject(TokenAuthServiceProxy);
    private _router = inject(Router);
    private _messageService = inject(MessageService);
    private _tokenService = inject(TokenService);
    private _logService = inject(LogService);
    private oauthService = inject(OAuthService);
    private spinnerService = inject(NgxSpinnerService);
    private _localStorageService = inject(LocalStorageService);
    private _twitterService = inject(TwitterServiceProxy);
    private _msalService = inject(MsalService);

    private MSAL: PublicClientApplication;

    static readonly twoFactorRememberClientTokenName = 'TwoFactorRememberClientToken';

    public passwordlessAuthenticateModel: PasswordlessAuthenticateModel;
    passwordlessAuthenticateResult: PasswordlessAuthenticateResultModel;

    authenticateModel: AuthenticateModel;
    authenticateResult: AuthenticateResultModel;
    externalLoginProviders: ExternalLoginProvider[] = [];
    rememberMe: boolean;

    wsFederationAuthenticationContext: any;
    localizationSourceName = AppConsts.localization.defaultLocalizationSourceName;

    localization: LocalizationService;

    constructor() {
        this.clear();
    }

    authenticate(finallyCallback?: () => void, redirectUrl?: string, captchaResponse?: string): void {
        finallyCallback =
            finallyCallback ||
            (() => {
                this.spinnerService.hide();
            });

        const self = this;
        this._localStorageService.getItem(LoginService.twoFactorRememberClientTokenName, function (err, value) {
            self.authenticateModel.twoFactorRememberClientToken = value?.token;
            self.authenticateModel.singleSignIn = UrlHelper.getSingleSignIn();
            self.authenticateModel.returnUrl = UrlHelper.getReturnUrl();
            self.authenticateModel.captchaResponse = captchaResponse;

            self._tokenAuthService.authenticate(self.authenticateModel).subscribe({
                next: (result: AuthenticateResultModel) => {
                    self.processAuthenticateResult(result, redirectUrl);
                    finallyCallback();
                },
                error: (err: any) => {
                    finallyCallback();
                },
            });
        });
    }

    passwordlessAuthenticate(
        finallyCallback?: () => void,
        passwordlessAuthenticateModel?: PasswordlessAuthenticateModel,
        redirectUrl?: string,
        captchaResponse?: string
    ): void {
        finallyCallback =
            finallyCallback ||
            (() => {
                this.spinnerService.hide();
            });

        const self = this;

        self._tokenAuthService.passwordlessAuthenticate(passwordlessAuthenticateModel).subscribe({
            next: (result: PasswordlessAuthenticateResultModel) => {
                self.processPasswordlessAuthenticateResult(result, redirectUrl);
                finallyCallback();
            },
            error: (err: any) => {
                finallyCallback();
            },
        });
    }

    ensureExternalLoginProviderInitialized(loginProvider: ExternalLoginProvider, callback: () => void) {
        if (loginProvider.initialized) {
            callback();
            return;
        }

        if (loginProvider.name === ExternalLoginProvider.FACEBOOK) {
            new ScriptLoaderService().load('//connect.facebook.net/en_US/sdk.js').then(() => {
                FB.init({
                    appId: loginProvider.clientId,
                    cookie: false,
                    xfbml: true,
                    version: 'v2.5',
                });

                FB.getLoginStatus((response) => {
                    this.facebookLoginStatusChangeCallback(response);
                    if (response.status !== 'connected') {
                        callback();
                    }
                });
            });
        } else if (loginProvider.name === ExternalLoginProvider.GOOGLE) {
            new ScriptLoaderService()
                .load('https://apis.google.com/js/api.js', 'https://accounts.google.com/gsi/client')
                .then(() => {
                    gapi.load('client', () => {
                        gapi.client.init({}).then(() => {
                            gapi.client.load('oauth2', 'v2', () => {
                                callback();
                            });
                        });
                    });
                });
        } else if (loginProvider.name === ExternalLoginProvider.MICROSOFT) {
            this.MSAL = new PublicClientApplication({
                auth: {
                    clientId: loginProvider.clientId,
                    redirectUri: AppConsts.appBaseUrl,
                },
            });
            callback();
        } else if (loginProvider.name === ExternalLoginProvider.OPENID) {
            const authConfig = this.getOpenIdConnectConfig(loginProvider);
            this.oauthService.configure(authConfig);
            this.oauthService.initLoginFlow('openIdConnect=1');
        } else if (loginProvider.name === ExternalLoginProvider.WSFEDERATION) {
            let config = this.getWsFederationConnectConfig(loginProvider);

            this.MSAL = new PublicClientApplication(config);
            (this._msalService as any).instance = this.MSAL;

            this.MSAL.initialize()
                .then(() => {
                    this._msalService.instance.clearCache();

                    return this.MSAL.loginPopup({
                        scopes: ['openid', 'profile', 'email'],
                    });
                })
                .then((response: AuthenticationResult) => {
                    callback();
                    this.wsFederationLoginStatusChangeCallback(response);
                })
                .catch((error) => {
                    console.error('Error during MSAL initialization or login:', error);
                });
        } else if (loginProvider.name === ExternalLoginProvider.TWITTER) {
            callback();
        }
    }

    public twitterLoginCallback(token: string, verifier: string) {
        this.spinnerService.show();

        this._twitterService.getAccessToken(token, verifier).subscribe((response) => {
            const model = new ExternalAuthenticateModel();
            model.authProvider = ExternalLoginProvider.TWITTER;
            model.providerAccessCode = response.accessToken + '&' + response.accessTokenSecret;
            model.providerKey = response.userId;
            model.singleSignIn = UrlHelper.getSingleSignIn();
            model.returnUrl = UrlHelper.getReturnUrl();

            this._tokenAuthService
                .externalAuthenticate(model)
                .pipe(
                    finalize(() => {
                        this.spinnerService.hide();
                    })
                )
                .subscribe((result: ExternalAuthenticateResultModel) => {
                    if (result.waitingForActivation) {
                        this._messageService.info(
                            this.localization.localize(
                                'SuccessfullyRegisteredWaitingForActivation',
                                this.localizationSourceName
                            )
                        );
                        return;
                    }

                    this.login(
                        result.accessToken,
                        result.encryptedAccessToken,
                        result.expireInSeconds,
                        result.refreshToken,
                        result.refreshTokenExpireInSeconds,
                        false,
                        '',
                        result.returnUrl
                    );
                });
        });
    }

    public wsFederationLoginStatusChangeCallback(authResult: AuthenticationResult) {
        const model = new ExternalAuthenticateModel();
        model.authProvider = ExternalLoginProvider.WSFEDERATION;
        model.providerAccessCode = authResult.idToken;
        model.providerKey = authResult.idTokenClaims['sub'];
        model.singleSignIn = UrlHelper.getSingleSignIn();
        model.returnUrl = UrlHelper.getReturnUrl();

        this._tokenAuthService.externalAuthenticate(model).subscribe((result: ExternalAuthenticateResultModel) => {
            if (result.waitingForActivation) {
                this._messageService.info(
                    this.localization.localize(
                        'SuccessfullyRegisteredWaitingForActivation',
                        this.localizationSourceName
                    )
                );
                this._router.navigate(['account/login']);
                return;
            }

            this.login(
                result.accessToken,
                result.encryptedAccessToken,
                result.expireInSeconds,
                result.refreshToken,
                result.refreshTokenExpireInSeconds,
                false,
                '',
                result.returnUrl
            );
        });
    }

    public openIdConnectLoginCallback(resp) {
        this.initExternalLoginProviders(() => {
            let openIdProvider = _filter(this.externalLoginProviders, {
                name: 'OpenIdConnect',
            })[0];
            let authConfig = this.getOpenIdConnectConfig(openIdProvider);

            this.oauthService.configure(authConfig);
            this.spinnerService.show();

            this.oauthService.loadDiscoveryDocumentAndTryLogin().then(() => {
                let claims = this.oauthService.getIdentityClaims();

                const model = new ExternalAuthenticateModel();
                model.authProvider = ExternalLoginProvider.OPENID;
                model.providerAccessCode = this.oauthService.getIdToken();
                model.providerKey = claims['sub'];
                model.singleSignIn = UrlHelper.getSingleSignIn();
                model.returnUrl = UrlHelper.getReturnUrl();

                this._tokenAuthService
                    .externalAuthenticate(model)
                    .pipe(
                        finalize(() => {
                            this.spinnerService.hide();
                        })
                    )
                    .subscribe((result: ExternalAuthenticateResultModel) => {
                        if (result.waitingForActivation) {
                            this._messageService.info(
                                this.localization.localize(
                                    'SuccessfullyRegisteredWaitingForActivation',
                                    this.localizationSourceName
                                )
                            );
                            return;
                        }

                        this.login(
                            result.accessToken,
                            result.encryptedAccessToken,
                            result.expireInSeconds,
                            result.refreshToken,
                            result.refreshTokenExpireInSeconds,
                            false,
                            '',
                            result.returnUrl
                        );
                    });
            });
        });
    }

    externalAuthenticate(provider: ExternalLoginProvider): void {
        this.ensureExternalLoginProviderInitialized(provider, () => {
            if (provider.name === ExternalLoginProvider.FACEBOOK) {
                FB.login(
                    (response) => {
                        this.facebookLoginStatusChangeCallback(response);
                    },
                    { scope: 'email' }
                );
            } else if (provider.name === ExternalLoginProvider.GOOGLE) {
                let gisGoogleTokenClient = google.accounts.oauth2.initTokenClient({
                    client_id: provider.clientId,
                    scope: 'openid profile email',
                    callback: (resp) => {
                        if (resp.error !== undefined) {
                            throw resp;
                        }

                        // GIS has automatically updated gapi.client with the newly issued access token
                        this.googleLoginStatusChangeCallback(resp);
                    },
                });

                // Conditionally ask users to select the Google Account they'd like to use,
                // and explicitly obtain their consent to fetch their Calendar.
                // NOTE: To request an access token a user gesture is necessary.
                if (gapi.client.getToken() === null) {
                    // Prompt the user to select an Google Account and asked for consent to share their data
                    // when establishing a new session.
                    gisGoogleTokenClient.requestAccessToken({ prompt: 'consent' });
                } else {
                    // Skip display of account chooser and consent dialog for an existing session.
                    gisGoogleTokenClient.requestAccessToken({ prompt: '' });
                }
            } else if (provider.name === ExternalLoginProvider.MICROSOFT) {
                let scopes = ['user.read'];
                this.spinnerService.show();

                this.MSAL.initialize().then(() => {
                    return this.MSAL.loginPopup({
                        scopes: scopes,
                    }).then((idTokenResponse: AuthenticationResult) => {
                        this.MSAL.acquireTokenSilent({
                            account: this.MSAL.getAllAccounts()[0],
                            scopes: scopes,
                        })
                            .then((accessTokenResponse: AuthenticationResult) => {
                                this.microsoftLoginCallback(accessTokenResponse);
                                this.spinnerService.hide();
                            })
                            .catch((error) => {
                                abp.log.error(error);
                                abp.message.error(
                                    this.localization.localize(
                                        'CouldNotValidateExternalUser',
                                        this.localizationSourceName
                                    )
                                );
                            });
                    });
                });
            } else if (provider.name === ExternalLoginProvider.TWITTER) {
                this.startTwitterLogin();
            }
        });
    }

    qrLoginAuthenticate(
        accessToken: string,
        encryptedAccessToken: string,
        expireInSeconds: number,
        refreshToken: string,
        refreshTokenExpireInSeconds: number
    ): void {
        this.login(
            accessToken,
            encryptedAccessToken,
            expireInSeconds,
            refreshToken,
            refreshTokenExpireInSeconds,
            false,
            '',
            ''
        );
    }

    init(): void {
        this.initExternalLoginProviders();
    }

    private processAuthenticateResult(authenticateResult: AuthenticateResultModel, redirectUrl?: string) {
        this.authenticateResult = authenticateResult;

        if (authenticateResult.shouldResetPassword) {
            // Password reset

            this._router.navigate(['account/reset-password'], {
                queryParams: {
                    c: authenticateResult.c,
                },
            });

            this.clear();
        } else if (authenticateResult.requiresTwoFactorVerification) {
            // Two factor authentication

            this._router.navigate(['account/send-code']);
        } else if (authenticateResult.accessToken) {
            // Successfully logged in

            if (authenticateResult.returnUrl && !redirectUrl) {
                redirectUrl = authenticateResult.returnUrl;
            }

            this.login(
                authenticateResult.accessToken,
                authenticateResult.encryptedAccessToken,
                authenticateResult.expireInSeconds,
                authenticateResult.refreshToken,
                authenticateResult.refreshTokenExpireInSeconds,
                this.rememberMe,
                authenticateResult.twoFactorRememberClientToken,
                redirectUrl
            );
        } else {
            // Unexpected result!

            this._logService.warn('Unexpected authenticateResult!');
            this._router.navigate(['account/login']);
        }
    }

    private processPasswordlessAuthenticateResult(
        passwordlessAuthenticateResult: PasswordlessAuthenticateResultModel,
        redirectUrl?: string
    ) {
        this.passwordlessAuthenticateResult = passwordlessAuthenticateResult;

        if (passwordlessAuthenticateResult.accessToken) {
            // Successfully logged in

            if (passwordlessAuthenticateResult.returnUrl && !redirectUrl) {
                redirectUrl = passwordlessAuthenticateResult.returnUrl;
            }

            this.login(
                passwordlessAuthenticateResult.accessToken,
                passwordlessAuthenticateResult.encryptedAccessToken,
                passwordlessAuthenticateResult.expireInSeconds,
                passwordlessAuthenticateResult.refreshToken,
                passwordlessAuthenticateResult.refreshTokenExpireInSeconds,
                this.rememberMe,
                null,
                redirectUrl
            );
        } else {
            // Unexpected result!

            this._logService.warn('Unexpected authenticateResult!');
            this._router.navigate(['account/login']);
        }
    }

    private login(
        accessToken: string,
        encryptedAccessToken: string,
        expireInSeconds: number,
        refreshToken: string,
        refreshTokenExpireInSeconds: number,
        rememberMe?: boolean,
        twoFactorRememberClientToken?: string,
        redirectUrl?: string
    ): void {
        let tokenExpireDate = rememberMe ? new Date(new Date().getTime() + 1000 * expireInSeconds) : undefined;

        this._tokenService.setToken(accessToken, tokenExpireDate);

        if (refreshToken && rememberMe) {
            let refreshTokenExpireDate = rememberMe
                ? new Date(new Date().getTime() + 1000 * refreshTokenExpireInSeconds)
                : undefined;
            this._tokenService.setRefreshToken(refreshToken, refreshTokenExpireDate);
        }

        let self = this;
        this._localStorageService.setItem(
            AppConsts.authorization.encrptedAuthTokenName,
            {
                token: encryptedAccessToken,
                expireDate: tokenExpireDate,
            },
            () => {
                if (twoFactorRememberClientToken) {
                    self._localStorageService.setItem(
                        LoginService.twoFactorRememberClientTokenName,
                        {
                            token: twoFactorRememberClientToken,
                            expireDate: new Date(new Date().getTime() + 365 * 86400000), // 1 year
                        },
                        () => {
                            self.redirectToLoginResult(redirectUrl);
                        }
                    );
                } else {
                    self.redirectToLoginResult(redirectUrl);
                }
            }
        );
    }

    private redirectToLoginResult(redirectUrl?: string): void {
        if (redirectUrl) {
            location.href = redirectUrl;
        } else {
            let initialUrl = UrlHelper.initialUrl;

            if (initialUrl.indexOf('/account') > 0) {
                initialUrl = AppConsts.appBaseUrl;
            }

            location.href = initialUrl;
        }
    }

    private clear(): void {
        this.authenticateModel = new AuthenticateModel();
        this.authenticateModel.rememberClient = false;
        this.authenticateResult = null;
        this.rememberMe = false;
    }

    private initExternalLoginProviders(callback?: any) {
        this._tokenAuthService
            .getExternalAuthenticationProviders()
            .subscribe((providers: ExternalLoginProviderInfoModel[]) => {
                this.externalLoginProviders = _map(providers, (p) => new ExternalLoginProvider(p));
                if (callback) {
                    callback();
                }
            });
    }

    private getWsFederationConnectConfig(loginProvider: ExternalLoginProvider): any {
        const config = {
            auth: {
                clientId: loginProvider.clientId,
                authority: loginProvider.additionalParams['Authority']
                    ? loginProvider.additionalParams['Authority']
                    : loginProvider.additionalParams['Tenant'] !== ''
                      ? `https://login.microsoftonline.com/${loginProvider.additionalParams['Tenant']}/v2.0`
                      : 'https://login.microsoftonline.com/common/v2.0',
                redirectUri: window.location.origin,
            },
            cache: {
                cacheLocation: 'localStorage',
                storeAuthStateInCookie: false,
            },
        };
        return config;
    }

    private getOpenIdConnectConfig(loginProvider: ExternalLoginProvider): AuthConfig {
        let authConfig = new AuthConfig();
        authConfig.loginUrl = loginProvider.additionalParams['LoginUrl'];
        authConfig.issuer = loginProvider.additionalParams['Authority'];
        authConfig.skipIssuerCheck = (loginProvider.additionalParams['ValidateIssuer'] || '').toLowerCase() === 'false';
        authConfig.clientId = loginProvider.clientId;
        authConfig.responseType = loginProvider.additionalParams['ResponseType'];
        authConfig.strictDiscoveryDocumentValidation = false;
        authConfig.redirectUri = window.location.origin + '/account/login';
        authConfig.scope = 'openid profile';
        authConfig.requestAccessToken = false;
        return authConfig;
    }

    private facebookLoginStatusChangeCallback(resp) {
        if (resp.status === 'connected') {
            const model = new ExternalAuthenticateModel();
            model.authProvider = ExternalLoginProvider.FACEBOOK;
            model.providerAccessCode = resp.authResponse.accessToken;
            model.providerKey = resp.authResponse.userID;
            model.singleSignIn = UrlHelper.getSingleSignIn();
            model.returnUrl = UrlHelper.getReturnUrl();

            this._tokenAuthService.externalAuthenticate(model).subscribe((result: ExternalAuthenticateResultModel) => {
                if (result.waitingForActivation) {
                    this._messageService.info(
                        this.localization.localize(
                            'SuccessfullyRegisteredWaitingForActivation',
                            this.localizationSourceName
                        )
                    );
                    return;
                }

                this.login(
                    result.accessToken,
                    result.encryptedAccessToken,
                    result.expireInSeconds,
                    result.refreshToken,
                    result.refreshTokenExpireInSeconds,
                    false,
                    '',
                    result.returnUrl
                );
            });
        }
    }

    private startTwitterLogin() {
        this.spinnerService.show();
        this._twitterService
            .getRequestToken()
            .pipe(finalize(() => this.spinnerService.hide()))
            .subscribe((result) => {
                if (result.confirmed) {
                    window.location.href = result.redirectUrl;
                } else {
                    this._messageService.error('Could not get twitter request token!');
                }
            });
    }

    private googleLoginStatusChangeCallback(response) {
        if (response.error !== undefined) {
            return;
        }

        let _$this = this;

        gapi.client.oauth2.userinfo.get().execute(function (resp) {
            const model = new ExternalAuthenticateModel();
            model.authProvider = ExternalLoginProvider.GOOGLE;
            model.providerAccessCode = response.access_token;
            model.providerKey = resp.id;
            model.singleSignIn = UrlHelper.getSingleSignIn();
            model.returnUrl = UrlHelper.getReturnUrl();

            _$this._tokenAuthService
                .externalAuthenticate(model)
                .subscribe((result: ExternalAuthenticateResultModel) => {
                    if (result.waitingForActivation) {
                        _$this._messageService.info(
                            _$this.localization.localize(
                                'SuccessfullyRegisteredWaitingForActivation',
                                _$this.localizationSourceName
                            )
                        );
                        return;
                    }

                    _$this.login(
                        result.accessToken,
                        result.encryptedAccessToken,
                        result.expireInSeconds,
                        result.refreshToken,
                        result.refreshTokenExpireInSeconds,
                        false,
                        '',
                        result.returnUrl
                    );
                });
        });
    }

    private microsoftLoginCallback(response: AuthenticationResult) {
        const model = new ExternalAuthenticateModel();
        model.authProvider = ExternalLoginProvider.MICROSOFT;
        model.providerAccessCode = response.accessToken;
        model.providerKey = response.uniqueId;
        model.singleSignIn = UrlHelper.getSingleSignIn();
        model.returnUrl = UrlHelper.getReturnUrl();

        this.spinnerService.show();

        this._tokenAuthService.externalAuthenticate(model).subscribe((result: ExternalAuthenticateResultModel) => {
            if (result.waitingForActivation) {
                this._messageService.info(
                    this.localization.localize(
                        'SuccessfullyRegisteredWaitingForActivation',
                        this.localizationSourceName
                    )
                );
                return;
            }

            this.login(
                result.accessToken,
                result.encryptedAccessToken,
                result.expireInSeconds,
                result.refreshToken,
                result.refreshTokenExpireInSeconds,
                false,
                '',
                result.returnUrl
            );
            this.spinnerService.hide();
        });
    }
}
