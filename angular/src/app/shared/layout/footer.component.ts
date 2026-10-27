import { Component, OnInit, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ThemeAssetContributorFactory } from '@shared/helpers/ThemeAssetContributorFactory';

@Component({
    templateUrl: './footer.component.html',
    selector: 'footer-bar',
    imports: [],
})
export class FooterComponent extends AppComponentBase implements OnInit {
    readonly useBottomDiv = input(true);

    /** HCMv3's own release number -- the one a tenant's installed version is compared against. */
    productVersion: string;
    footerStyle = 'footer py-4 d-flex flex-lg-column';

    ngOnInit(): void {
        this.productVersion = this.appSession.application?.productVersion ?? '';

        let themeAssetContributor = ThemeAssetContributorFactory.getCurrent();
        if (themeAssetContributor) {
            this.footerStyle = themeAssetContributor.getFooterStyle();
        }
    }
}
