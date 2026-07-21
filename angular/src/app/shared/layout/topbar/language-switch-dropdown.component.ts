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

    ngOnInit(): void {
        this.languages = _filter(this.localization.languages, (l) => l.isDisabled === false);
        this.currentLanguage = this.localization.currentLanguage;
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
