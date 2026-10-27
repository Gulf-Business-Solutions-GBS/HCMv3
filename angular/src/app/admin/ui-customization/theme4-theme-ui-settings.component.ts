import { Component, inject, input } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ThemeSettingsDto, UiCustomizationSettingsServiceProxy } from '@shared/service-proxies/service-proxies';

import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './theme4-theme-ui-settings.component.html',
    animations: [appModuleAnimation()],
    selector: 'theme4-theme-ui-settings',
    imports: [TabsetComponent, TabDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class Theme4ThemeUiSettingsComponent extends AppComponentBase {
    private _uiCustomizationService = inject(UiCustomizationSettingsServiceProxy);

    readonly settings = input<ThemeSettingsDto>(undefined);

    getCustomizedSetting(settings: ThemeSettingsDto) {
        settings.theme = 'theme4';

        return settings;
    }

    updateDefaultUiManagementSettings(): void {
        this._uiCustomizationService
            .updateDefaultUiManagementSettings(this.getCustomizedSetting(this.settings()))
            .subscribe(() => {
                window.location.reload();
            });
    }

    updateUiManagementSettings(): void {
        this._uiCustomizationService
            .updateUiManagementSettings(this.getCustomizedSetting(this.settings()))
            .subscribe(() => {
                window.location.reload();
            });
    }

    useSystemDefaultSettings(): void {
        this._uiCustomizationService.useSystemDefaultSettings().subscribe(() => {
            window.location.reload();
        });
    }
}
