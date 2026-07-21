import { Component, OnInit, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { filter as _filter } from 'lodash-es';

@Component({
    selector: 'language-switch',
    templateUrl: './language-switch.component.html',
    styles: ['.language-switch-btn { width: auto; height: auto; }'],
    imports: [],
})
export class LanguageSwitchComponent extends AppComponentBase implements OnInit {
    readonly currentLanguage = signal<abp.localization.ILanguageInfo>(null);
    readonly languages = signal<abp.localization.ILanguageInfo[]>([]);

    ngOnInit(): void {
        this.languages.set(_filter(abp.localization.languages, (l) => (<any>l).isDisabled === false));
        this.currentLanguage.set(abp.localization.currentLanguage);
    }

    changeLanguage(language: abp.localization.ILanguageInfo) {
        abp.utils.setCookieValue(
            'Abp.Localization.CultureName',
            language.name,
            new Date(new Date().getTime() + 5 * 365 * 86400000), // 5 year
            abp.appPath
        );

        location.reload();
    }
}
