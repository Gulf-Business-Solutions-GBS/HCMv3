import { Injectable, inject } from '@angular/core';
import { ReferenceDataServiceProxy } from '@shared/service-proxies/service-proxies';
import { GoLiveStateService } from './go-live-state.service';
import { LookupOptionMap } from './lookup-options';

/**
 * Loads the dropdown values that every `optionsFrom` in the registry refers to, and publishes them to
 * the state service.
 *
 * One request for all of them: the payload is a few kilobytes of names, and the step screens need
 * several lists at once.
 */
@Injectable({ providedIn: 'root' })
export class GoLiveLookupsService {
    private readonly state = inject(GoLiveStateService);
    private readonly proxy = inject(ReferenceDataServiceProxy);

    private loading = false;

    /** Safe to call repeatedly: concurrent calls collapse. */
    load(): void {
        if (this.loading) {
            return;
        }
        this.loading = true;

        this.proxy.getLookupOptions().subscribe({
            next: (lists) => {
                this.loading = false;

                const lookups: LookupOptionMap = {};
                for (const list of lists ?? []) {
                    if (list.lookup) {
                        lookups[list.lookup] = list.options ?? [];
                    }
                }

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
}
