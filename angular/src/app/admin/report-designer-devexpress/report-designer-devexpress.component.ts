import { Component } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DxReportDesignerModule } from 'devexpress-reporting-angular/dx-report-designer';

/**
 * Hosts the real DevExpress Web Report Designer (the official `devexpress-reporting-angular` components,
 * not an iframe).
 *
 * The designer talks to the DevExpress backend controllers hosted by Web.Host under the conventional
 * default route prefixes (`/DXXRD/*` for the designer, `/DXXRDV/*` for the document viewer used by the
 * designer's Preview tab, `/DXXQB/*` for the query builder). Those routes live on the API origin, NOT on
 * the Angular dev-server origin, so `host` is derived from the app's configured remote service base URL
 * rather than hardcoded.
 *
 * NOTE: the Dx components are non-standalone, so this standalone component imports `DxReportDesignerModule`
 * rather than the component classes directly.
 */
@Component({
    templateUrl: './report-designer-devexpress.component.html',
    styleUrl: './report-designer-devexpress.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, DxReportDesignerModule],
})
export class ReportDesignerDevExpressComponent extends AppComponentBase {
    /**
     * Backend origin for the DevExpress services. DevExpress concatenates `host` + action path, so it must
     * carry a trailing slash (e.g. `https://localhost:44301/`).
     */
    readonly host: string = AppConsts.remoteServiceBaseUrl.endsWith('/')
        ? AppConsts.remoteServiceBaseUrl
        : AppConsts.remoteServiceBaseUrl + '/';

    /** Backend route prefix registered for the Report Designer controller. */
    readonly getDesignerModelAction: string = 'DXXRD/GetDesignerModel';

    /** Name of the report the backend's report storage resolves. */
    readonly reportUrl: string = 'PayslipEn';
}
