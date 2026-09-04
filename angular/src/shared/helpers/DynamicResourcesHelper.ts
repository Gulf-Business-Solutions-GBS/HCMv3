import { AppConsts } from '@shared/AppConsts';
import * as rtlDetect from 'rtl-detect';
import { StyleLoaderService } from '@shared/utils/style-loader.service';
import { ThemeHelper } from '@app/shared/layout/themes/ThemeHelper';
import { ThemeAssetContributorFactory } from './ThemeAssetContributorFactory';

export class DynamicResourcesHelper {
    static loadResources(callback: () => void): void {
        Promise.all([DynamicResourcesHelper.loadStyles()]).then(() => {
            callback();
        });
    }

    static loadStyles(): Promise<any> {
        let theme = ThemeHelper.getTheme();

        const isRtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);

        if (isRtl) {
            document.documentElement.setAttribute('dir', 'rtl');
        }

        const skin = ThemeHelper.darkMode() ? 'dark' : 'light';
        document.documentElement.setAttribute('data-bs-theme', skin);

        const cssPostfix = isRtl ? '.rtl' : '';
        const styleLoaderService = new StyleLoaderService();

        // Which theme's Metronic BUNDLE to load, which is not always the theme itself -- see below.
        const bundleTheme = DynamicResourcesHelper.getMetronicBundleTheme(theme);

        let styleUrls = [
            AppConsts.appBaseUrl +
                '/assets/metronic/themes/' +
                bundleTheme +
                '/css/style.bundle' +
                cssPostfix.replace('-', '.') +
                '.css',
            AppConsts.appBaseUrl +
                '/assets/metronic/themes/' +
                bundleTheme +
                '/plugins/global/plugins.bundle' +
                cssPostfix.replace('-', '.') +
                '.css',
            AppConsts.appBaseUrl + '/assets/primeng/datatable/css/primeng.datatable' + cssPostfix + '.min.css', // PrimeNG RTL styles
            AppConsts.appBaseUrl + '/assets/common/styles/metronic-customize.min.css',
            AppConsts.appBaseUrl + '/assets/common/styles/themes/' + theme + '/metronic-customize.min.css',
            AppConsts.appBaseUrl + '/assets/common/styles/metronic-customize-angular.min.css',
        ].concat(DynamicResourcesHelper.getAdditionalThemeAssets());

        DynamicResourcesHelper.setBodyAttributes();

        if (isRtl) {
            styleUrls.push(AppConsts.appBaseUrl + '/assets/common/styles/abp-zero-template-rtl.min.css');
        }

        styleLoaderService.loadArray(styleUrls).then(function () {
            abp.event.trigger('app.dynamic-styles-loaded');
        });

        return Promise.resolve(true);
    }

    /**
     * The theme whose prebuilt Metronic bundle a theme uses.
     *
     * Every Metronic theme ships 22 MB of prebuilt CSS, JS and plugins, and theme14 is the default theme
     * in different colours -- the same layout, the same components, the same behaviour. Copying those
     * 22 MB to say the same thing twice would put a second identical bundle in every deployment and in
     * the repository, and the two would then have to be patched in step on every Metronic upgrade.
     *
     * So theme14 borrows the default bundle, and its palette arrives as one small stylesheet on top.
     * Any future colour-only theme should map here too rather than duplicate the bundle.
     */
    static getMetronicBundleTheme(theme: string): string {
        return theme === 'theme14' ? 'default' : theme;
    }

    static getAdditionalThemeAssets(): string[] {
        let assetContributor = ThemeAssetContributorFactory.getCurrent();
        if (!assetContributor) {
            return [];
        }

        return assetContributor.getAssetUrls();
    }

    static setBodyAttributes(): void {
        let assetContributor = ThemeAssetContributorFactory.getCurrent();
        if (!assetContributor) {
            return;
        }

        let attributes = assetContributor.getBodyAttributes();
        for (let i = 0; i < attributes.length; i++) {
            let attr = attributes[i];
            document.body.setAttribute(attr.name, attr.value);
        }
    }
}
