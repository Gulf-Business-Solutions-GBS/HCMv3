import { IThemeAssetContributor } from '../ThemeAssetContributor';
import { AppConsts } from '@shared/AppConsts';
import { NameValuePair } from '@shared/utils/name-value-pair';
import { ThemeHelper } from '../ThemeHelper';

export class Theme7ThemeAssetContributor implements IThemeAssetContributor {
    public getAssetUrls(): string[] {
        return [AppConsts.appBaseUrl + '/assets/fonts/fonts-roboto.min.css'];
    }

    public getMenuWrapperStyle(): string {
        return '';
    }

    public getSubheaderStyle(): string {
        return 'text-dark fw-bold my-1 me-5';
    }

    public getFooterStyle(): string {
        return 'footer py-4 d-flex flex-lg-column';
    }

    getBodyAttributes(): NameValuePair[] {
        return [];
    }

    getAppModuleBodyClass(): string {
        let defaultBodyClass = 'header-fixed header-tablet-and-mobile-fixed aside-fixed aside-secondary-enabled';

        if (ThemeHelper.getDesktopFixedFooter() === 'true') {
            defaultBodyClass += ' ' + 'footer-fixed';
        }
        if (ThemeHelper.getMobileFixedFooter() === 'true') {
            defaultBodyClass += ' ' + 'footer-tablet-and-mobile-fixed';
        }

        return defaultBodyClass;
    }
}
