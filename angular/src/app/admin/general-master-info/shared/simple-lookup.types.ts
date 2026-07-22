import { Type } from '@angular/core';
import { Observable } from 'rxjs';
import {
    GetAllSimpleLookupsInput,
    CreateOrEditSimpleLookupDto,
    PagedResultDtoOfSimpleLookupDto,
} from '@shared/service-proxies/service-proxies';

/**
 * Structural contract every generated `*ServiceProxy` for a SimpleLookupAppServiceBase-derived
 * app service satisfies automatically (TypeScript structural typing) — no adapter needed.
 * See HCMv3.Application/GeneralMasterInfo/SimpleLookupAppServiceBase.cs for the backend contract.
 */
export interface ISimpleLookupServiceProxy {
    getAll(input: GetAllSimpleLookupsInput | undefined): Observable<PagedResultDtoOfSimpleLookupDto>;
    getForEdit(id: number | undefined): Observable<CreateOrEditSimpleLookupDto>;
    createOrEdit(input: CreateOrEditSimpleLookupDto | undefined): Observable<void>;
    delete(id: number | undefined): Observable<void>;
}

/** Route `data` shape consumed by SimpleLookupComponent / SimpleLookupModalComponent. */
export interface SimpleLookupRouteData {
    permission: string;
    createPermission: string;
    editPermission: string;
    deletePermission: string;
    /** Localization key for the page title / entity display name, e.g. "Countries". */
    titleKey: string;
    serviceProxyType: Type<ISimpleLookupServiceProxy>;
}
