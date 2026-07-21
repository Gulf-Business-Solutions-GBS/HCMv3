import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    SettingScopes,
    SendTestEmailInput,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    JsonClaimMapDto,
} from '@shared/service-proxies/service-proxies';
import { FileUpload, FileUploadModule } from 'primeng/fileupload';
import { finalize } from 'rxjs/operators';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { UntypedFormControl, FormsModule } from '@angular/forms';
import { LogoService } from '@app/shared/common/logo/logo.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgClass } from '@angular/common';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { TimeZoneComboComponent } from '../../shared/common/timing/timezone-combo.component';
import { Tooltip } from 'primeng/tooltip';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { PasswordInputWithShowButtonComponent } from '../../shared/common/password-input-with-show-button/password-input-with-show-button.component';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { HttpClient } from '@angular/common/http';

@Component({
    templateUrl: './tenant-settings.component.html',
    styleUrls: ['./tenant-settings.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        TimeZoneComboComponent,
        FormsModule,
        Tooltip,
        FileUploadModule,
        NgClass,
        ValidationMessagesComponent,
        PasswordInputWithShowButtonComponent,
        KeyValueListManagerComponent_1,
        LocalizePipe,
    ],
})
export class TenantSettingsComponent extends AppComponentBase implements OnInit {
    private _tenantSettingsService = inject(TenantSettingsServiceProxy);
    private _httpClient = inject(HttpClient);
    private _logoService = inject(LogoService);

    readonly wsFederationClaimsMappingManager = viewChild<KeyValueListManagerComponent>(
        'wsFederationClaimsMappingManager'
    );
    readonly openIdConnectClaimsMappingManager = viewChild<KeyValueListManagerComponent>(
        'openIdConnectClaimsMappingManager'
    );
    readonly emailSmtpSettingsForm = viewChild<UntypedFormControl>('emailSmtpSettingsForm');
    readonly securitySettingsForm = viewChild<UntypedFormControl>('securitySettingsForm');

    readonly darkLogoUpload = viewChild<FileUpload>('darkLogoUpload');
    readonly darkLogoMinimalUpload = viewChild<FileUpload>('darkLogoMinimalUpload');
    readonly lightLogoUpload = viewChild<FileUpload>('lightLogoUpload');
    readonly lightLogoMinimalUpload = viewChild<FileUpload>('lightLogoMinimalUpload');
    readonly customCssUpload = viewChild<FileUpload>('customCssUpload');

    readonly usingDefaultTimeZone = signal(false);
    readonly initialTimeZone = signal<string | null>(null);
    readonly testEmailAddress = signal<string | undefined>(undefined);
    readonly setRandomPassword = signal(false);

    readonly isMultiTenancyEnabled = signal(this.multiTenancy.isEnabled);
    readonly showTimezoneSelection = signal(abp.clock.provider.supportsMultipleTimezone);
    readonly activeTabIndex = signal(abp.clock.provider.supportsMultipleTimezone ? 0 : 1);
    readonly loading = signal(false);
    readonly settings = signal<TenantSettingsEditDto | undefined>(undefined);

    remoteServiceBaseUrl = AppConsts.remoteServiceBaseUrl;

    defaultTimezoneScope: SettingScopes = SettingScopes.Tenant;

    readonly enabledSocialLoginSettings = signal<string[]>([]);
    readonly useFacebookHostSettings = signal(false);
    readonly useGoogleHostSettings = signal(false);
    readonly useMicrosoftHostSettings = signal(false);
    readonly useWsFederationHostSettings = signal(false);
    readonly useOpenIdConnectHostSettings = signal(false);
    readonly useTwitterHostSettings = signal(false);

    wsFederationClaimMappings: { key: string; value: string }[];
    openIdConnectClaimMappings: { key: string; value: string }[];
    readonly openIdConnectResponseTypeCode = signal(false);
    readonly openIdConnectResponseTypeToken = signal(false);
    readonly openIdConnectResponseTypeIdToken = signal(false);

    readonly initialEmailSettings = signal('');

    darkLogoUploadUrl: string;
    darkLogoMinimalUploadUrl: string;
    lightLogoUploadUrl: string;
    lightLogoMinimalUploadUrl: string;
    customCssUploadUrl: string;

    readonly darkLogoUrl = signal('');
    readonly darkSmallLogoUrl = signal('');
    readonly lightLogoUrl = signal('');
    readonly lightSmallLogoUrl = signal('');

    emailDomainPattern = '^[a-zA-Z0-9._%+-]+(?<!@)[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$';

    constructor() {
        super();
        this.initLogoUrls();
        this.initUploadUrls();
    }

    ngOnInit(): void {
        this.testEmailAddress.set(this.appSession.user.emailAddress);
        this.getSettings();
        this.loadSocialLoginSettings();
    }

    initUploadUrls(): void {
        const baseUrl = AppConsts.remoteServiceBaseUrl;
        this.darkLogoUploadUrl = baseUrl + '/TenantCustomization/UploadDarkLogo';
        this.darkLogoMinimalUploadUrl = baseUrl + '/TenantCustomization/UploadDarkLogoMinimal';
        this.lightLogoUploadUrl = baseUrl + '/TenantCustomization/UploadLightLogo';
        this.lightLogoMinimalUploadUrl = baseUrl + '/TenantCustomization/UploadLightLogoMinimal';
        this.customCssUploadUrl = baseUrl + '/TenantCustomization/UploadCustomCss';
    }

    getSettings(): void {
        this.loading.set(true);
        this._tenantSettingsService
            .getAllSettings()
            .pipe(
                finalize(() => {
                    this.loading.set(false);
                })
            )
            .subscribe((result: TenantSettingsEditDto) => {
                this.settings.set(result);
                if (result.general) {
                    this.initialTimeZone.set(result.general.timezone);
                    this.usingDefaultTimeZone.set(
                        result.general.timezoneForComparison === abp.setting.values['Abp.Timing.TimeZone']
                    );
                }
                this.useFacebookHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.facebook.appId ||
                        result.externalLoginProviderSettings.facebook_IsDeactivated
                    )
                );
                this.useGoogleHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.google.clientId ||
                        result.externalLoginProviderSettings.google_IsDeactivated
                    )
                );
                this.useMicrosoftHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.microsoft.clientId ||
                        result.externalLoginProviderSettings.microsoft_IsDeactivated
                    )
                );
                this.useWsFederationHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.wsFederation.clientId ||
                        result.externalLoginProviderSettings.wsFederation_IsDeactivated
                    )
                );
                this.useOpenIdConnectHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.openIdConnect.clientId ||
                        result.externalLoginProviderSettings.openIdConnect_IsDeactivated
                    )
                );
                this.useTwitterHostSettings.set(
                    !(
                        result.externalLoginProviderSettings.twitter.consumerKey ||
                        result.externalLoginProviderSettings.twitter_IsDeactivated
                    )
                );

                this.wsFederationClaimMappings = result.externalLoginProviderSettings.openIdConnectClaimsMapping.map(
                    (item) => ({
                        key: item.key,
                        value: item.claim,
                    })
                );

                this.openIdConnectClaimMappings = result.externalLoginProviderSettings.openIdConnectClaimsMapping.map(
                    (item) => ({
                        key: item.key,
                        value: item.claim,
                    })
                );

                if (result.externalLoginProviderSettings.openIdConnect.responseType) {
                    let openIdConnectResponseTypes =
                        result.externalLoginProviderSettings.openIdConnect.responseType.split(',');

                    this.openIdConnectResponseTypeCode.set(openIdConnectResponseTypes.indexOf('code') > -1);
                    this.openIdConnectResponseTypeIdToken.set(openIdConnectResponseTypes.indexOf('id_token') > -1);
                    this.openIdConnectResponseTypeToken.set(openIdConnectResponseTypes.indexOf('token') > -1);
                }

                this.initialEmailSettings.set(JSON.stringify(result.email));
            });
    }

    initLogoUrls(): void {
        this.setLogoUrlBySkin('dark');
        this.setLogoUrlBySkin('dark-sm');
        this.setLogoUrlBySkin('light');
        this.setLogoUrlBySkin('light-sm');
    }

    uploadDarkLogo(event: { files: File[] }): void {
        const file = event.files[0];
        this.uploadFile(file, this.darkLogoUploadUrl, this.darkLogoUpload(), (response) => {
            this.appSession.tenant.darkLogoFileType = response.result.fileType;
            this.appSession.tenant.darkLogoId = response.result.id;
            this.setLogoUrlBySkin('dark');
            this._logoService.notifyLogoChanged('dark');
        });
    }

    uploadDarkLogoMinimal(event: { files: File[] }): void {
        const file = event.files[0];
        this.uploadFile(file, this.darkLogoMinimalUploadUrl, this.darkLogoMinimalUpload(), (response) => {
            this.appSession.tenant.darkLogoMinimalFileType = response.result.fileType;
            this.appSession.tenant.darkLogoMinimalId = response.result.id;
            this.setLogoUrlBySkin('dark-sm');
            this._logoService.notifyLogoChanged('dark-sm');
        });
    }

    uploadLightLogo(event: { files: File[] }): void {
        const file = event.files[0];
        this.uploadFile(file, this.lightLogoUploadUrl, this.lightLogoUpload(), (response) => {
            this.appSession.tenant.lightLogoFileType = response.result.fileType;
            this.appSession.tenant.lightLogoId = response.result.id;
            this.setLogoUrlBySkin('light');
            this._logoService.notifyLogoChanged('light');
        });
    }

    uploadLightLogoMinimal(event: { files: File[] }): void {
        const file = event.files[0];
        this.uploadFile(file, this.lightLogoMinimalUploadUrl, this.lightLogoMinimalUpload(), (response) => {
            this.appSession.tenant.lightLogoMinimalFileType = response.result.fileType;
            this.appSession.tenant.lightLogoMinimalId = response.result.id;
            this.setLogoUrlBySkin('light-sm');
            this._logoService.notifyLogoChanged('light-sm');
        });
    }

    uploadCustomCss(event: { files: File[] }): void {
        const file = event.files[0];
        this.uploadFile(file, this.customCssUploadUrl, this.customCssUpload(), (response) => {
            this.appSession.tenant.customCssId = response.result.id;

            let oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }

            let tenantCustomCss = document.createElement('link');
            tenantCustomCss.setAttribute('id', 'TenantCustomCss');
            tenantCustomCss.setAttribute('rel', 'stylesheet');
            tenantCustomCss.setAttribute(
                'href',
                AppConsts.remoteServiceBaseUrl +
                    '/TenantCustomization/GetCustomCss?tenantId=' +
                    this.appSession.tenant.id
            );
            document.head.appendChild(tenantCustomCss);
        });
    }

    onBeforeSend(event): void {
        event.xhr.setRequestHeader('Authorization', 'Bearer ' + abp.auth.getToken());
    }

    clearDarkLogo(): void {
        this._tenantSettingsService.clearDarkLogo().subscribe(() => {
            this.appSession.tenant.darkLogoFileType = null;
            this.appSession.tenant.darkLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('dark');
            this._logoService.notifyLogoChanged('dark');
        });
    }

    clearDarkLogoMinimal(): void {
        this._tenantSettingsService.clearDarkLogoMinimal().subscribe(() => {
            this.appSession.tenant.darkLogoMinimalFileType = null;
            this.appSession.tenant.darkLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('dark-sm');
            this._logoService.notifyLogoChanged('dark-sm');
        });
    }

    clearLightLogo(): void {
        this._tenantSettingsService.clearLightLogo().subscribe(() => {
            this.appSession.tenant.lightLogoFileType = null;
            this.appSession.tenant.lightLogoId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('light');
            this._logoService.notifyLogoChanged('light');
        });
    }

    clearLightLogoMinimal(): void {
        this._tenantSettingsService.clearLightLogoMinimal().subscribe(() => {
            this.appSession.tenant.lightLogoMinimalFileType = null;
            this.appSession.tenant.lightLogoMinimalId = null;
            this.notify.info(this.l('ClearedSuccessfully'));
            this.setLogoUrlBySkin('light-sm');
            this._logoService.notifyLogoChanged('light-sm');
        });
    }

    clearCustomCss(): void {
        this._tenantSettingsService.clearCustomCss().subscribe(() => {
            this.appSession.tenant.customCssId = null;

            let oldTenantCustomCss = document.getElementById('TenantCustomCss');
            if (oldTenantCustomCss) {
                oldTenantCustomCss.remove();
            }

            this.notify.info(this.l('ClearedSuccessfully'));
        });
    }

    mapClaims(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }

        const wsFederationClaimsMappingManager = this.wsFederationClaimsMappingManager();
        if (wsFederationClaimsMappingManager) {
            settings.externalLoginProviderSettings.wsFederationClaimsMapping = wsFederationClaimsMappingManager
                .getItems()
                .map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        })
                );
        }

        const openIdConnectClaimsMappingManager = this.openIdConnectClaimsMappingManager();
        if (openIdConnectClaimsMappingManager) {
            settings.externalLoginProviderSettings.openIdConnectClaimsMapping = openIdConnectClaimsMappingManager
                .getItems()
                .map(
                    (item) =>
                        new JsonClaimMapDto({
                            key: item.key,
                            claim: item.value,
                        })
                );
        }
    }

    saveAll(): void {
        if (!this.isSmtpSettingsFormValid()) {
            return;
        }

        if (!this.isSecuritySettingsFormValid()) {
            return;
        }

        const settings = this.settings();
        if (!settings) {
            return;
        }

        settings.externalLoginProviderSettings.openIdConnect.responseType =
            this.getSelectedOpenIdConnectResponseTypes();

        this.mapClaims();
        this._tenantSettingsService.updateAllSettings(settings).subscribe(() => {
            this.notify.info(this.l('SavedSuccessfully'));

            if (
                abp.clock.provider.supportsMultipleTimezone &&
                this.usingDefaultTimeZone() &&
                this.initialTimeZone() !== settings.general.timezone
            ) {
                this.message.info(this.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                    window.location.reload();
                });
            }
            this.initialEmailSettings.set(JSON.stringify(settings.email));
        });
    }

    getSelectedOpenIdConnectResponseTypes(): string {
        let openIdConnectResponseTypes = '';
        if (this.openIdConnectResponseTypeToken()) {
            openIdConnectResponseTypes += 'token';
        }

        if (this.openIdConnectResponseTypeIdToken()) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'id_token';
        }

        if (this.openIdConnectResponseTypeCode()) {
            if (openIdConnectResponseTypes.length > 0) {
                openIdConnectResponseTypes += ',';
            }
            openIdConnectResponseTypes += 'code';
        }

        return openIdConnectResponseTypes;
    }

    sendTestEmail(): void {
        const input = new SendTestEmailInput();
        input.emailAddress = this.testEmailAddress();
        const settings = this.settings();

        if (this.initialEmailSettings() !== JSON.stringify(settings?.email)) {
            this.message.confirm(this.l('SendEmailWithSavedSettingsWarning'), this.l('AreYouSure'), (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantSettingsService.sendTestEmail(input).subscribe((result) => {
                        this.notify.info(this.l('TestEmailSentSuccessfully'));
                    });
                }
            });
        } else {
            this._tenantSettingsService.sendTestEmail(input).subscribe((result) => {
                this.notify.info(this.l('TestEmailSentSuccessfully'));
            });
        }
    }

    loadSocialLoginSettings(): void {
        this._tenantSettingsService.getEnabledSocialLoginSettings().subscribe((setting) => {
            this.enabledSocialLoginSettings.set(setting.enabledSocialLoginSettings);
        });
    }

    clearFacebookSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.facebook.appId = '';
        settings.externalLoginProviderSettings.facebook.appSecret = '';
        settings.externalLoginProviderSettings.facebook_IsDeactivated = false;
    }

    clearGoogleSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.google.clientId = '';
        settings.externalLoginProviderSettings.google.clientSecret = '';
        settings.externalLoginProviderSettings.google.userInfoEndpoint = '';
        settings.externalLoginProviderSettings.google_IsDeactivated = false;
    }

    clearMicrosoftSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.microsoft.clientId = '';
        settings.externalLoginProviderSettings.microsoft.clientSecret = '';
        settings.externalLoginProviderSettings.microsoft_IsDeactivated = false;
    }

    clearWsFederationSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.wsFederation.clientId = '';
        settings.externalLoginProviderSettings.wsFederation.authority = '';
        settings.externalLoginProviderSettings.wsFederation.wtrealm = '';
        settings.externalLoginProviderSettings.wsFederation.metaDataAddress = '';
        settings.externalLoginProviderSettings.wsFederation.tenant = '';
        settings.externalLoginProviderSettings.wsFederationClaimsMapping = [];
        settings.externalLoginProviderSettings.wsFederation_IsDeactivated = false;
    }

    clearOpenIdSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.openIdConnect.clientId = '';
        settings.externalLoginProviderSettings.openIdConnect.clientSecret = '';
        settings.externalLoginProviderSettings.openIdConnect.authority = '';
        settings.externalLoginProviderSettings.openIdConnect.loginUrl = '';
        settings.externalLoginProviderSettings.openIdConnectClaimsMapping = [];
        settings.externalLoginProviderSettings.openIdConnect_IsDeactivated = false;
    }

    clearTwitterSettings(): void {
        const settings = this.settings();
        if (!settings) {
            return;
        }
        settings.externalLoginProviderSettings.twitter.consumerKey = '';
        settings.externalLoginProviderSettings.twitter.consumerSecret = '';
    }

    isSocialLoginEnabled(name: string): boolean {
        const enabledSettings = this.enabledSocialLoginSettings();
        return enabledSettings && enabledSettings.indexOf(name) !== -1;
    }

    isSmtpSettingsFormValid(): boolean {
        const emailSmtpSettingsForm = this.emailSmtpSettingsForm();
        if (!emailSmtpSettingsForm) {
            return true;
        }
        return emailSmtpSettingsForm.valid;
    }

    isSecuritySettingsFormValid(): boolean {
        return this.securitySettingsForm().valid;
    }

    private uploadFile(
        file: File,
        uploadUrl: string,
        fileUploadComponent: FileUpload,
        onSuccess: (response: any) => void
    ): void {
        const formData: FormData = new FormData();
        formData.append('file', file, file.name);

        this._httpClient
            .post<any>(uploadUrl, formData)
            .pipe(finalize(() => fileUploadComponent.clear()))
            .subscribe({
                next: (response) => {
                    onSuccess(response);
                    this.notify.info(this.l('SavedSuccessfully'));
                },
                error: (error) => {
                    this.message.error(error?.error?.message);
                },
            });
    }

    private setLogoUrlBySkin(skin: string): void {
        const baseLogoUrl = this._logoService.getLogoUrl(skin);
        const urlWithTimestamp = baseLogoUrl + '?t=' + new Date().getTime();

        switch (skin) {
            case 'dark':
                this.darkLogoUrl.set(urlWithTimestamp);
                break;
            case 'dark-sm':
                this.darkSmallLogoUrl.set(urlWithTimestamp);
                break;
            case 'light':
                this.lightLogoUrl.set(urlWithTimestamp);
                break;
            case 'light-sm':
                this.lightSmallLogoUrl.set(urlWithTimestamp);
                break;
        }
    }
}
