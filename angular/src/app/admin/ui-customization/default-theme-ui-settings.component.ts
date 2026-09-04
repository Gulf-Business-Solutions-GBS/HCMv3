import { Component, computed, inject, input } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ThemeSettingsDto, UiCustomizationSettingsServiceProxy } from '@shared/service-proxies/service-proxies';

import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './default-theme-ui-settings.component.html',
    animations: [appModuleAnimation()],
    selector: 'default-theme-ui-settings',
    imports: [TabsetComponent, TabDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class DefaultThemeUiSettingsComponent extends AppComponentBase {
    private _uiCustomizationService = inject(UiCustomizationSettingsServiceProxy);

    readonly settings = input<ThemeSettingsDto>(undefined);

    /**
     * Which theme this panel is saving for. Defaults to 'default', which is every existing caller.
     *
     * NEEDED BECAUSE THE THEME NAME IS STAMPED ON SAVE, below. Theme14 -- the legacy self-service skin --
     * is the default layout in different colours and so reuses this panel; without this input the stamp
     * rewrote its name back to 'default' on every save, so the theme could be chosen but never stuck,
     * and no colour ever changed.
     */
    readonly themeName = input<string>('default');

    AsideMinimizingDisabled = computed(() => !this.settings().menu.allowAsideMinimizing);

    getCustomizedSetting(settings: ThemeSettingsDto) {
        settings.theme = this.themeName();

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

    allowAsideMinimizingChange(val): void {
        if (!val) {
            this.settings().menu.defaultMinimizedAside = false;
        }
    }
}
