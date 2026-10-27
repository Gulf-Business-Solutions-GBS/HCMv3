import { PlatformLocation, registerLocaleData } from '@angular/common';
import { Injector } from '@angular/core';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { AppConsts } from '@shared/AppConsts';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { PrimeNG } from 'primeng/config';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import {
    UiCustomizationSettingsDto,
    ThemeSettingsDto,
    ThemeMenuSettingsDto,
    ThemeLayoutSettingsDto,
    ThemeHeaderSettingsDto,
    ThemeSubHeaderSettingsDto,
    ThemeFooterSettingsDto,
    ApplicationInfoDto,
} from '@shared/service-proxies/service-proxies';
import localforage from 'localforage';
import { AppPreBootstrap } from './AppPreBootstrap';
import { DomHelper } from '@shared/helpers/DomHelper';
import { CookieConsentService } from '@shared/common/session/cookie-consent.service';
import { LocaleMappingService } from '@shared/locale-mapping.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

export function appInitializerFactory(injector: Injector, platformLocation: PlatformLocation) {
    return () => {
        let spinnerService = injector.get(NgxSpinnerService);

        spinnerService.show();

        return new Promise<boolean>((resolve, reject) => {
            AppConsts.appBaseHref = getBaseHref(platformLocation);
            let appBaseUrl = getDocumentOrigin() + AppConsts.appBaseHref;

            initializeLocalForage();

            AppPreBootstrap.run(appBaseUrl, injector, resolve, reject, (isDatabaseExist: boolean) => {
                handleLogoutRequest(injector.get(AppAuthService));

                if (isDatabaseExist === false) {
                    doConfigurationForInstallPage(injector);
                    spinnerService.hide();
                    resolve(true);
                } else {
                    let appSessionService: AppSessionService = injector.get(AppSessionService);
                    appSessionService.init().then(
                        (result) => {
                            initializeAppCssClasses(injector, result);
                            initializeTenantResources(injector);
                            initializeCookieConsent(injector);
                            registerLocales(resolve, reject, spinnerService, injector);
                        },
                        (err) => {
                            spinnerService.hide();
                            reject(err);
                        }
                    );
                }
            });
        });
    };
}

function initializeLocalForage() {
    localforage.config({
        driver: localforage.LOCALSTORAGE,
        name: 'HCMv3',
        version: 1.0,
        storeName: 'abpzerotemplate_local_storage',
        description: 'Cached data for HCMv3',
    });
}

function getDefaultThemeForInstallPage(): UiCustomizationSettingsDto {
    let theme = new UiCustomizationSettingsDto();
    theme.baseSettings = new ThemeSettingsDto();
    theme.baseSettings.theme = 'default';
    theme.baseSettings.menu = new ThemeMenuSettingsDto();
    theme.baseSettings.menu.asideSkin = 'light';

    theme.baseSettings.header = new ThemeHeaderSettingsDto();

    theme.baseSettings.subHeader = new ThemeSubHeaderSettingsDto();

    theme.baseSettings.layout = new ThemeLayoutSettingsDto();
    theme.baseSettings.layout.layoutType = 'fluid';
    theme.baseSettings.header = new ThemeHeaderSettingsDto();
    theme.baseSettings.footer = new ThemeFooterSettingsDto();
    return theme;
}

function setApplicationInfoForInstallPage(injector, theme: UiCustomizationSettingsDto) {
    let appSessionService: AppSessionService = injector.get(AppSessionService);
    let dateTimeService: DateTimeService = injector.get(DateTimeService);
    appSessionService.theme = theme;
    appSessionService.application = new ApplicationInfoDto();
    appSessionService.application.releaseDate = dateTimeService.getStartOfDay().toDateTime();
}

function doConfigurationForInstallPage(injector) {
    let theme = getDefaultThemeForInstallPage();
    setApplicationInfoForInstallPage(injector, theme);

    initializeAppCssClasses(injector, theme);
}

function initializeAppCssClasses(injector: Injector, theme: UiCustomizationSettingsDto) {
    let appUiCustomizationService = injector.get(AppUiCustomizationService);
    appUiCustomizationService.init(theme);

    // Css classes based on the layout
    if (abp.session.userId) {
        document.body.className = appUiCustomizationService.getAppModuleBodyClass();
        document.body.setAttribute('style', appUiCustomizationService.getAppModuleBodyStyle());
    } else {
        document.body.className = appUiCustomizationService.getAccountModuleBodyClass();
        document.body.setAttribute('style', appUiCustomizationService.getAccountModuleBodyStyle());
    }
}

function initializeTenantResources(injector: Injector) {
    let appSessionService: AppSessionService = injector.get(AppSessionService);

    if (appSessionService.tenant && appSessionService.tenant.customCssId) {
        document.head.appendChild(
            DomHelper.createElement('link', [
                {
                    key: 'id',
                    value: 'TenantCustomCss',
                },
                {
                    key: 'rel',
                    value: 'stylesheet',
                },
                {
                    key: 'href',
                    value:
                        AppConsts.remoteServiceBaseUrl +
                        '/TenantCustomization/GetCustomCss?tenantId=' +
                        encodeURIComponent(appSessionService.tenant.id),
                },
            ])
        );
    }

    let metaImage = DomHelper.getElementByAttributeValue('meta', 'property', 'og:image');
    if (metaImage) {
        //set og share image meta tag
        if (!appSessionService.tenant || !appSessionService.tenant.HasLogo()) {
            let ui: AppUiCustomizationService = injector.get(AppUiCustomizationService);
            metaImage.setAttribute(
                'content',
                window.location.origin +
                    '/assets/common/images/app-logo-on-' +
                    abp.setting.get(
                        appSessionService.theme.baseSettings.theme + '.' + 'App.UiManagement.Left.AsideSkin'
                    ) +
                    '.svg'
            );
        } else {
            metaImage.setAttribute(
                'content',
                AppConsts.remoteServiceBaseUrl + '/TenantCustomization/GetLogo?tenantId=' + appSessionService.tenant.id
            );
        }
    }
}

function initializeCookieConsent(injector: Injector) {
    let cookieConsentService: CookieConsentService = injector.get(CookieConsentService);
    cookieConsentService.init();
}

function getDocumentOrigin() {
    if (!document.location.origin) {
        return (
            document.location.protocol +
            '//' +
            document.location.hostname +
            (document.location.port ? ':' + document.location.port : '')
        );
    }

    return document.location.origin;
}

function registerLocales(
    resolve: (value?: boolean | Promise<boolean>) => void,
    reject: any,
    spinnerService: NgxSpinnerService,
    injector: Injector
) {
    if (shouldLoadLocale()) {
        let angularLocale = convertAbpLocaleToAngularLocale(abp.localization.currentLanguage.name);
        import(`../node_modules/@angular/common/locales/${angularLocale}.js`).then((module) => {
            registerLocaleData(module.default);
            registerPrimeNGLocale(injector).then((_) => {
                resolve(true);
                spinnerService.hide();
            });
        }, reject);
    } else {
        registerPrimeNGLocale(injector).then((_) => {
            resolve(true);
            spinnerService.hide();
        });
    }
}

async function registerPrimeNGLocale(injector: Injector): Promise<void> {
    const primeNGLocale = convertAbpLocaleToPrimeNGLocale(abp.localization.currentLanguage.name);

    if (primeNGLocale === 'en') {
        return;
    }

    try {
        const localeModule = await import(`../node_modules/primelocale/${primeNGLocale}.json`);
        const primeNGConfig = injector.get(PrimeNG);
        const translations = localeModule.default[primeNGLocale] || localeModule.default;
        primeNGConfig.setTranslation(translations);
    } catch (error) {
        console.warn(`PrimeNG locale '${primeNGLocale}' could not be loaded. Using default English.`);
    }
}

export function convertAbpLocaleToPrimeNGLocale(locale: string): string {
    return new LocaleMappingService().map('primeng', locale);
}

export function shouldLoadLocale(): boolean {
    return abp.localization.currentLanguage.name && abp.localization.currentLanguage.name !== 'en-US';
}

export function convertAbpLocaleToAngularLocale(locale: string): string {
    return new LocaleMappingService().map('angular', locale);
}

export function getRemoteServiceBaseUrl(): string {
    return AppConsts.remoteServiceBaseUrl;
}

export function getCurrentLanguage(): string {
    return convertAbpLocaleToAngularLocale(abp.localization.currentLanguage.name);
}

export function getCurrencyCode(injector: Injector): string {
    let appSessionService: AppSessionService = injector.get(AppSessionService);
    return appSessionService.application.currency;
}

export function getBaseHref(platformLocation: PlatformLocation): string {
    let baseUrl = platformLocation.getBaseHrefFromDOM();
    if (baseUrl) {
        return baseUrl;
    }

    return '/';
}

function handleLogoutRequest(authService: AppAuthService) {
    let currentUrl = UrlHelper.initialUrl;
    let returnUrl = UrlHelper.getReturnUrl();
    if (currentUrl.indexOf('account/logout') >= 0 && returnUrl) {
        authService.logout(true, returnUrl);
    }
}
