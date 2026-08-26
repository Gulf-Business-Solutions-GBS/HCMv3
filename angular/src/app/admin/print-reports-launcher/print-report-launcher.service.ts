import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

/**
 * Row shape returned by the backend `PrintReportLauncherAppService.GetRunnableReports`.
 *
 * NOTE: this is a hand-written mirror of the backend DTO. The NSwag proxy
 * (`PrintReportLauncherServiceProxy` / `RunnablePrintReportDto`) does not exist yet - proxies are
 * regenerated centrally once the backend lands. Once `angular/nswag/refresh.bat` has been run, this
 * interface and the service below can be deleted and the components can inject the generated proxy
 * instead; the field names below are deliberately identical to the agreed contract so that swap is
 * a pure import change.
 */
export interface RunnablePrintReport {
    id: number;
    code: string;
    nameEn: string;
    nameAr: string;
    /** DevExpress report identifier - this is what is passed as `reportUrl` to the viewer/designer. */
    reportName: string;
    reportCategory: string;
    /** False when the catalog lists the report but its report class is not ported yet. */
    isAvailable: boolean;
}

/**
 * Thin stand-in for the not-yet-generated NSwag proxy. It calls the same ABP dynamic Web API endpoint
 * the proxy will call and unwraps ABP's `{ result: ... }` envelope. Auth/tenant headers are added by
 * the globally registered `ZeroTemplateHttpInterceptor`.
 */
@Injectable({ providedIn: 'root' })
export class PrintReportLauncherService {
    private _http = inject(HttpClient);

    private get baseUrl(): string {
        return AppConsts.remoteServiceBaseUrl.replace(/\/+$/, '');
    }

    getRunnableReports(): Observable<RunnablePrintReport[]> {
        return this._http
            .get<any>(this.baseUrl + '/api/services/app/PrintReportLauncher/GetRunnableReports')
            .pipe(map((response) => (response?.result?.items ?? response?.result ?? []) as RunnablePrintReport[]));
    }
}
