import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';

/**
 * TEMPORARY stand-in for the NSwag-generated `BankFilesServiceProxy`.
 *
 * WHY THIS EXISTS: the backend app service
 * (`HCMv3.Application.Shared/PayrollManagement/IBankFilesAppService.cs`) is complete, but
 * `angular/src/shared/service-proxies/service-proxies.ts` was deliberately NOT regenerated during this wave
 * -- several agents were working concurrently and an NSwag pass would have clobbered their proxies.
 *
 * ACTION REQUIRED after a consolidated `angular/nswag/refresh.bat` run:
 *   1. delete this file and `BankFileTypes` in ./bank-file.types.ts,
 *   2. in bank-file.component.ts import `BankFilesServiceProxy` + the generated DTOs from
 *      `@shared/service-proxies/service-proxies` and inject the proxy instead of this service,
 *   3. wrap the generate payload in `new GenerateBankFileInput({ ... })`.
 * The property names below already match the generated DTOs exactly, so no other change is needed.
 */
@Injectable({ providedIn: 'root' })
export class BankFileApiService {
    private _http = inject(HttpClient);

    private get baseUrl(): string {
        return AppConsts.remoteServiceBaseUrl;
    }

    get<T>(action: string): Observable<T> {
        return this._http.get<any>(`${this.baseUrl}/api/services/app/BankFiles/${action}`).pipe(unwrap<T>());
    }

    post<T>(action: string, body: unknown): Observable<T> {
        return this._http.post<any>(`${this.baseUrl}/api/services/app/BankFiles/${action}`, body).pipe(unwrap<T>());
    }
}

/** ABP wraps every dynamic-API response in `{ result, success, error }`. */
function unwrap<T>() {
    return map((response: any) => (response && 'result' in response ? response.result : response) as T);
}
