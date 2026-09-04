import { AppConsts } from '@shared/AppConsts';
import { NameValuePair } from '@shared/utils/name-value-pair';
import { IThemeAssetContributor } from '../ThemeAssetContributor';
import { DefaultThemeAssetContributor } from '../default/DefaultThemeAssetContributor';

/**
 * The legacy Pinnacle HRMS self-service skin.
 *
 * EVERYTHING EXCEPT THE STYLESHEET IS THE DEFAULT THEME'S. It extends the default contributor rather
 * than restating its body classes and wrapper styles, so the two layouts cannot drift apart -- which
 * matters more here than anywhere, because the promise of this theme is that switching back changes
 * nothing but the colours.
 *
 * Zain is loaded from Google Fonts with a real fallback stack. If the font fails to load -- an offline
 * deployment, a blocked CDN -- the palette still applies and the app is merely in the system sans, which
 * is a cosmetic loss rather than a broken page.
 */
export class Theme14ThemeAssetContributor extends DefaultThemeAssetContributor implements IThemeAssetContributor {
    public getAssetUrls(): string[] {
        return [
            'https://fonts.googleapis.com/css2?family=Zain:wght@300;400;700;800&display=swap',
            AppConsts.appBaseUrl + '/assets/common/styles/themes/theme14/legacy-ess-skin.css',
        ];
    }

    getBodyAttributes(): NameValuePair[] {
        // Marks the document for the skin stylesheet, so every rule in it is scoped to this theme and
        // cannot leak into the other thirteen.
        return super.getBodyAttributes().concat([{ name: 'data-hcm-skin', value: 'legacy-ess' }]);
    }
}
