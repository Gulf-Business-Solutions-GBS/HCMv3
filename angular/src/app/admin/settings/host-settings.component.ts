import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    SettingScopes,
    HostSettingsEditDto,
    HostSettingsServiceProxy,
    SendTestEmailInput,
    JsonClaimMapDto,
    SubscribableEditionComboboxItemDto,
} from '@shared/service-proxies/service-proxies';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';
import { UntypedFormControl, FormsModule } from '@angular/forms';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { NgClass } from '@angular/common';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { TimeZoneComboComponent } from '../../shared/common/timing/timezone-combo.component';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { PasswordInputWithShowButtonComponent } from '../../shared/common/password-input-with-show-button/password-input-with-show-button.component';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './host-settings.component.html',
    styleUrls: ['./host-settings.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        TimeZoneComboComponent,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        PasswordInputWithShowButtonComponent,
        KeyValueListManagerComponent_1,
        LocalizePipe,
    ],
})
export class HostSettingsComponent extends AppComponentBase implements OnInit {
    private _hostSettingService = inject(HostSettingsServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);

    readonly wsFederationClaimsMappingManager = viewChild<KeyValueListManagerComponent>(
        'wsFederationClaimsMappingManager'
    );
    readonly openIdConnectClaimsMappingManager = viewChild<KeyValueListManagerComponent>(
        'openIdConnectClaimsMappingManager'
    );
    readonly emailSmtpSettingsForm = viewChild<UntypedFormControl>('emailSmtpSettingsForm');
    readonly userManagementSettingsForm = viewChild<UntypedFormControl>('userManagementSettingsForm');
    readonly securitySettingsForm = viewChild<UntypedFormControl>('securitySettingsForm');

    loading = signal(false);
    hostSettings = signal<HostSettingsEditDto | undefined>(undefined);
    editions = signal<SubscribableEditionComboboxItemDto[] | undefined>(undefined);
    testEmailAddress = signal<string | undefined>(undefined);
    showTimezoneSelection = abp.clock.provider.supportsMultipleTimezone;
    defaultTimezoneScope: SettingScopes = SettingScopes.Application;

    usingDefaultTimeZone = signal(false);
    initialTimeZone = signal<string | undefined>(undefined);

    enabledSocialLoginSettings = signal<string[]>([]);

    wsFederationClaimMappings = signal<{ key: string; value: string }[]>([]);
    openIdConnectClaimMappings = signal<{ key: string; value: string }[]>([]);
    initialEmailSettings = signal<string>('');

    openIdConnectResponseTypeCode = signal(false);
    openIdConnectResponseTypeToken = signal(false);
    openIdConnectResponseTypeIdToken = signal(false);

    loadHostSettings(): void {
        const self = this;
        self._hostSettingService.getAllSettings().subscribe((setting) => {
            self.hostSettings.set(setting);
            self.initialTimeZone.set(setting.general.timezone);
            self.usingDefaultTimeZone.set(
                setting.general.timezoneForComparison === self.setting.get('Abp.Timing.TimeZone')
            );

            self.wsFederationClaimMappings.set(
                setting.externalLoginProviderSettings.wsFederationClaimsMapping.map((item) => ({
                    key: item.key,
                    value: item.claim,
                }))
            );
            self.openIdConnectClaimMappings.set(
                setting.externalLoginProviderSettings.openIdConnectClaimsMapping.map((item) => ({
                    key: item.key,
                    value: item.claim,
                }))
            );

            if (setting.externalLoginProviderSettings.openIdConnect.responseType) {
                let openIdConnectResponseTypes =
                    setting.externalLoginProviderSettings.openIdConnect.responseType.split(',');

                self.openIdConnectResponseTypeCode.set(openIdConnectResponseTypes.indexOf('code') > -1);
                self.openIdConnectResponseTypeIdToken.set(openIdConnectResponseTypes.indexOf('id_token') > -1);
                self.openIdConnectResponseTypeToken.set(openIdConnectResponseTypes.indexOf('token') > -1);
            }

            self.initialEmailSettings.set(JSON.stringify(setting.email));
        });
    }

    loadEditions(): void {
        const self = this;
        self._commonLookupService.getEditionsForCombobox(false).subscribe((result) => {
            const items = result.items ? [...result.items] : [];

            const notAssignedEdition = new SubscribableEditionComboboxItemDto();
            notAssignedEdition.value = null;
            notAssignedEdition.displayText = self.l('NotAssigned');
            notAssignedEdition.isFree = undefined;

            items.unshift(notAssignedEdition);
            self.editions.set(items);
        });
    }

    init(): void {
        const self = this;
        self.testEmailAddress.set(self.appSession.user.emailAddress);
        self.showTimezoneSelection = abp.clock.provider.supportsMultipleTimezone;
        self.loadHostSettings();
        self.loadEditions();
        self.loadSocialLoginSettings();
    }

    ngOnInit(): void {
        const self = this;
        self.init();
    }

    sendTestEmail(): void {
        const self = this;
        const input = new SendTestEmailInput();
        input.emailAddress = self.testEmailAddress();

        const hostSettingsValue = self.hostSettings();
        if (self.initialEmailSettings() !== JSON.stringify(hostSettingsValue.email)) {
            self.message.confirm(self.l('SendEmailWithSavedSettingsWarning'), self.l('AreYouSure'), (isConfirmed) => {
                if (isConfirmed) {
                    self._hostSettingService.sendTestEmail(input).subscribe((result) => {
                        self.notify.info(self.l('TestEmailSentSuccessfully'));
                    });
                }
            });
        } else {
            self._hostSettingService.sendTestEmail(input).subscribe((result) => {
                self.notify.info(self.l('TestEmailSentSuccessfully'));
            });
        }
    }

    mapClaims(): void {
        const hostSettingsValue = this.hostSettings();
        const wsFederationClaimsMappingManager = this.wsFederationClaimsMappingManager();
        if (wsFederationClaimsMappingManager) {
            hostSettingsValue.externalLoginProviderSettings.wsFederationClaimsMapping = wsFederationClaimsMappingManager
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
            hostSettingsValue.externalLoginProviderSettings.openIdConnectClaimsMapping =
                openIdConnectClaimsMappingManager.getItems().map(
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

        if (!this.isUserManagementSettingsFormValid()) {
            return;
        }

        if (!this.isSecuritySettingsFormValid()) {
            return;
        }

        const self = this;
        const hostSettingsValue = self.hostSettings();

        hostSettingsValue.externalLoginProviderSettings.openIdConnect.responseType =
            self.getSelectedOpenIdConnectResponseTypes();

        self.mapClaims();
        if (
            !hostSettingsValue.tenantManagement.defaultEditionId ||
            hostSettingsValue.tenantManagement.defaultEditionId.toString() === 'null'
        ) {
            hostSettingsValue.tenantManagement.defaultEditionId = null;
        }

        self._hostSettingService.updateAllSettings(hostSettingsValue).subscribe((result) => {
            self.notify.info(self.l('SavedSuccessfully'));

            if (
                abp.clock.provider.supportsMultipleTimezone &&
                self.usingDefaultTimeZone() &&
                self.initialTimeZone() !== hostSettingsValue.general.timezone
            ) {
                self.message.info(self.l('TimeZoneSettingChangedRefreshPageNotification')).then(() => {
                    window.location.reload();
                });
            }

            self.initialEmailSettings.set(JSON.stringify(hostSettingsValue.email));
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

    loadSocialLoginSettings(): void {
        const self = this;
        self._hostSettingService.getEnabledSocialLoginSettings().subscribe((setting) => {
            self.enabledSocialLoginSettings.set(setting.enabledSocialLoginSettings);
        });
    }

    isSocialLoginEnabled(name: string): boolean {
        const enabledSettings = this.enabledSocialLoginSettings();
        return enabledSettings && enabledSettings.indexOf(name) !== -1;
    }

    isSmtpSettingsFormValid(): boolean {
        return this.emailSmtpSettingsForm().valid;
    }

    isUserManagementSettingsFormValid(): boolean {
        return this.userManagementSettingsForm().valid;
    }

    isSecuritySettingsFormValid(): boolean {
        return this.securitySettingsForm().valid;
    }
}
