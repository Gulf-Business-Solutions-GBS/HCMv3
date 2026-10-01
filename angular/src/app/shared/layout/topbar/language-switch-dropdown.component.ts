import { Component, OnInit, inject, input } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { ChangeUserLanguageDto, ProfileServiceProxy } from '@shared/service-proxies/service-proxies';
import { filter as _filter } from 'lodash-es';

@Component({
    selector: 'language-switch-dropdown',
    templateUrl: './language-switch-dropdown.component.html',
    imports: [],
})
export class LanguageSwitchDropdownComponent extends ThemesLayoutBaseComponent implements OnInit {
    private _profileServiceProxy = inject(ProfileServiceProxy);

    readonly isDropup = input(false);
    readonly customStyle = input(
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative'
    );

    languages: abp.localization.ILanguageInfo[];
    currentLanguage: abp.localization.ILanguageInfo;

    /** Deduplicated by the code shown, so "en" and "en-GB" do not both render as EN. The current language wins. */
    switchableLanguages: abp.localization.ILanguageInfo[] = [];

    ngOnInit(): void {
        this.languages = _filter(this.localization.languages, (l) => l.isDisabled === false);
        this.currentLanguage = this.localization.currentLanguage;

        const byCode = new Map<string, abp.localization.ILanguageInfo>();
        for (const language of this.languages) {
            const code = this.shortCode(language);
            if (!byCode.has(code) || language.name === this.currentLanguage?.name) {
                byCode.set(code, language);
            }
        }
        this.switchableLanguages = Array.from(byCode.values());
    }

    /** "en-GB" -> EN, "ar" -> AR. The part before the dash is the language; the rest is the country. */
    shortCode(language: abp.localization.ILanguageInfo): string {
        return (language.name || '').split('-')[0].toUpperCase();
    }

    changeLanguage(languageName: string): void {
        const input = new ChangeUserLanguageDto();
        input.languageName = languageName;

        this._profileServiceProxy.changeLanguage(input).subscribe(() => {
            abp.utils.setCookieValue(
                'Abp.Localization.CultureName',
                languageName,
                new Date(new Date().getTime() + 5 * 365 * 86400000), //5 year
                abp.appPath
            );

            window.location.reload();
        });
    }
}
