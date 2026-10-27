import { Component, OnInit, computed, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { filter as _filter } from 'lodash-es';

/**
 * The language switch on the sign-in pages: EN | AR as text.
 *
 * NO FLAGS. A flag names a country, not a language. The Arabic here is not Kuwait's alone, and English
 * was represented by whichever of four flags -- us, gb, and two more -- happened to sit in the list.
 * Two letters say which language you are choosing and nothing else.
 *
 * WHICH languages appear is not decided here. It follows what an administrator has enabled in
 * Administration > Languages, so adding a third language is configuration rather than a code change.
 * The seeder now creates only English (UK) and Arabic on a fresh database.
 */
@Component({
    selector: 'language-switch',
    templateUrl: './language-switch.component.html',
    styleUrls: ['./language-switch.component.css'],
    imports: [],
})
export class LanguageSwitchComponent extends AppComponentBase implements OnInit {
    readonly currentLanguage = signal<abp.localization.ILanguageInfo>(null);
    readonly languages = signal<abp.localization.ILanguageInfo[]>([]);

    /**
     * Deduplicated by the code shown, because "en" and "en-GB" would both render as EN and give the
     * reader two identical-looking choices. The current language wins so the active code is the one
     * actually in use.
     */
    readonly switchableLanguages = computed<abp.localization.ILanguageInfo[]>(() => {
        const current = this.currentLanguage();
        const byCode = new Map<string, abp.localization.ILanguageInfo>();

        for (const language of this.languages()) {
            const code = this.shortCode(language);
            if (!byCode.has(code) || language.name === current?.name) {
                byCode.set(code, language);
            }
        }

        return Array.from(byCode.values());
    });

    ngOnInit(): void {
        this.currentLanguage.set(abp.localization.currentLanguage);
        this.languages.set(_filter(abp.localization.languages, (l) => (<any>l).isDisabled === false));
    }

    /** "en-GB" -> EN, "ar" -> AR. The part before the dash is the language; the rest is the country. */
    shortCode(language: abp.localization.ILanguageInfo): string {
        return (language.name || '').split('-')[0].toUpperCase();
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
