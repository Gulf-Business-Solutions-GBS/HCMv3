import { Component, inject } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * Embeds the standalone Stimulsoft Report Designer dev tool (aspnet-core/tools/HCMv3.ReportDesigner) in an
 * iframe so it is reachable from inside the main app's nav instead of a separate localhost port.
 *
 * This is NOT a production reporting feature: the embedded tool is a local-only, unauthenticated ASP.NET
 * Core MVC app used to review the DevExpress-to-Stimulsoft report migration. It must be started manually
 * (`dotnet run --project aspnet-core/tools/HCMv3.ReportDesigner`) - this component never attempts to start
 * it, and shows only a warning banner plus whatever the iframe renders (including a connection error if the
 * tool isn't running).
 */
@Component({
    templateUrl: './report-designer.component.html',
    styleUrl: './report-designer.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe],
})
export class ReportDesignerComponent extends AppComponentBase {
    private _sanitizer = inject(DomSanitizer);

    private readonly _designerUrl = 'http://localhost:5000/Designer/Index';

    readonly designerUrl: SafeResourceUrl = this._sanitizer.bypassSecurityTrustResourceUrl(this._designerUrl);
}
