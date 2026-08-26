import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DxReportViewerModule } from 'devexpress-reporting-angular/dx-report-viewer';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * Runtime viewer for a single print report.
 *
 * The DevExpress Web Document Viewer renders the report's OWN parameter panel (Payroll Area, Employee
 * Code, Month, Year, Run Type, Employee Status, ...) from the report definition - the panel is not
 * hand-built here. The viewer talks to the `DXXRDV` controller hosted by Web.Host, which lives on the
 * API origin rather than the Angular dev-server origin, so `host` is derived from the configured
 * remote service base URL.
 *
 * The Dx components are non-standalone, so this standalone component imports `DxReportViewerModule`
 * rather than the component classes directly.
 */
@Component({
    templateUrl: './print-report-view.component.html',
    styleUrl: './print-report-view.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, DxReportViewerModule],
})
export class PrintReportViewComponent extends AppComponentBase {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);

    /**
     * Backend origin for the DevExpress services. DevExpress concatenates `host` + action path, so it
     * must carry a trailing slash (e.g. `https://localhost:44301/`).
     */
    readonly host: string = AppConsts.remoteServiceBaseUrl.endsWith('/')
        ? AppConsts.remoteServiceBaseUrl
        : AppConsts.remoteServiceBaseUrl + '/';

    /** Backend route prefix registered for the Web Document Viewer controller. */
    readonly invokeAction: string = 'DXXRDV';

    /** Name of the report the backend's report storage resolves; comes from the route. */
    readonly reportUrl: string = this._route.snapshot.paramMap.get('reportName') ?? '';

    /**
     * Expand the right-hand tab panel and select the Parameters tab so the user immediately sees the
     * report's parameter editors instead of a collapsed strip.
     */
    onBeforeRender(event: any): void {
        try {
            const previewModel = event?.args;
            const tabPanel = previewModel?.tabPanel;

            if (!tabPanel) {
                return;
            }

            tabPanel.collapsed = false;

            const parametersTab = (tabPanel.tabs ?? []).find(
                (tab: any) => tab && previewModel.parametersModel && tab.model === previewModel.parametersModel
            );

            if (parametersTab) {
                parametersTab.active = true;
            }
        } catch {
            // Panel customization is cosmetic - never let it break report rendering.
        }
    }

    onServerError(event: any): void {
        const error = event?.args?.Error;
        this.message.error(error?.message ?? this.l('AnErrorOccurred'));
    }

    back(): void {
        this._router.navigate(['/app/admin/print-reports-launcher']);
    }
}
