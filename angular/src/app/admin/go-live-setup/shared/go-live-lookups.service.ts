import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
    BanksServiceProxy,
    CurrenciesServiceProxy,
    GetAllBanksInput,
    GetAllCurrenciesInput,
    GetAllSimpleLookupsInput,
    GetAllNationalitiesInput,
    GetAllWorkLocationsInput,
    JobTitlesServiceProxy,
    NationalitiesServiceProxy,
    WorkLocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GoLiveStateService } from './go-live-state.service';
import { LookupOptionMap } from './lookup-options';

/** The shape every lookup list shares once NSwag has generated it. */
interface NamedRows {
    items?: { nameEn?: string | undefined }[] | undefined;
}

/**
 * Loads the dropdown values that every `optionsFrom` in the registry refers to, and publishes them to
 * the state service.
 *
 * PROXY NOTE. The backend already exposes a single `ReferenceData/GetLookupOptions` endpoint returning
 * all of these in one request. It is not called here yet because the running Web.Host predates it, so
 * NSwag has not generated the method. After the next `angular/nswag/refresh.bat` against a restarted
 * host, the body of `load()` collapses to one `getLookupOptions()` call and every proxy import above
 * goes away. Nothing outside this file changes.
 *
 * Until then it fans out to the five lookup services the registry actually references, not the fourteen
 * the endpoint covers.
 */
@Injectable({ providedIn: 'root' })
export class GoLiveLookupsService {
    /** One page big enough for the largest seeded list -- 254 nationalities. */
    private static readonly PAGE_SIZE = 1000;

    private readonly state = inject(GoLiveStateService);
    private readonly nationalities = inject(NationalitiesServiceProxy);
    private readonly banks = inject(BanksServiceProxy);
    private readonly jobTitles = inject(JobTitlesServiceProxy);
    private readonly workLocations = inject(WorkLocationsServiceProxy);
    private readonly currencies = inject(CurrenciesServiceProxy);

    private loading = false;

    /**
     * Fetches every list and publishes them. Safe to call repeatedly: concurrent calls collapse, and a
     * list that fails resolves to empty so one broken lookup cannot leave every dropdown unpopulated.
     */
    load(): void {
        if (this.loading) {
            return;
        }
        this.loading = true;

        const nationalityInput = new GetAllNationalitiesInput();
        nationalityInput.maxResultCount = GoLiveLookupsService.PAGE_SIZE;
        nationalityInput.skipCount = 0;

        const bankInput = new GetAllBanksInput();
        bankInput.maxResultCount = GoLiveLookupsService.PAGE_SIZE;
        bankInput.skipCount = 0;

        const jobTitleInput = new GetAllSimpleLookupsInput();
        jobTitleInput.maxResultCount = GoLiveLookupsService.PAGE_SIZE;
        jobTitleInput.skipCount = 0;

        const workLocationInput = new GetAllWorkLocationsInput();
        workLocationInput.maxResultCount = GoLiveLookupsService.PAGE_SIZE;
        workLocationInput.skipCount = 0;

        const currencyInput = new GetAllCurrenciesInput();
        currencyInput.maxResultCount = GoLiveLookupsService.PAGE_SIZE;
        currencyInput.skipCount = 0;

        forkJoin({
            Nationality: this.names(this.nationalities.getAll(nationalityInput)),
            Bank: this.names(this.banks.getAll(bankInput)),
            JobTitle: this.names(this.jobTitles.getAll(jobTitleInput)),
            WorkLocation: this.names(this.workLocations.getAll(workLocationInput)),
            Currency: this.names(this.currencies.getAll(currencyInput)),
        }).subscribe({
            next: (lookups: LookupOptionMap) => {
                this.loading = false;
                this.state.setLookups(lookups);
            },
            error: () => {
                this.loading = false;
                // Publishing an empty map is deliberate: resolveColumnOptions turns an unpopulated
                // lookup into a free-text column rather than a dropdown the customer cannot escape.
                this.state.setLookups({});
            },
        });
    }

    private names(source: Observable<NamedRows>): Observable<string[]> {
        return source.pipe(
            map((result) =>
                (result.items ?? [])
                    .map((i) => i.nameEn?.trim())
                    .filter((n): n is string => !!n)
                    .sort((a, b) => a.localeCompare(b))
            ),
            catchError(() => of<string[]>([]))
        );
    }
}
