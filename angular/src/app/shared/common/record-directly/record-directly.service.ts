import { Injectable, inject } from '@angular/core';
import { PermissionCheckerService } from 'abp-ng2-module';
import { DirectEntryServiceProxy } from '@shared/service-proxies/service-proxies';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';

/** The permission that decides whether the bypass exists for this user at all. */
export const RecordWithoutApprovalPermission = 'Pages.Workflow.RecordWithoutApproval';

/**
 * Knows which transaction types the signed-in user may record without approval.
 *
 * <p>Answered once per session and shared. Thirteen admin forms ask the same question, and the answer
 * changes only when an administrator edits the catalog -- so one request, replayed, rather than one per
 * dialog opening.</p>
 */
@Injectable({ providedIn: 'root' })
export class RecordDirectlyService {
    private _proxy = inject(DirectEntryServiceProxy);
    private _permission = inject(PermissionCheckerService);

    private _codes$: Observable<string[]> | undefined;

    /**
     * True when this user may record <paramref name="requestType"/> as final.
     *
     * <p>Without the permission the answer is no and the endpoint is never called -- it would be refused,
     * and an error in the console on every form opening is worse than no question asked.</p>
     */
    canRecordDirectly(requestType: string): Observable<boolean> {
        if (!this._permission.isGranted(RecordWithoutApprovalPermission)) {
            return of(false);
        }

        if (!this._codes$) {
            this._codes$ = this._proxy.getDirectEntryRequestTypes().pipe(
                // A failure here must not stop a form opening. The user simply does not see the choice, and
                // the save routes for approval as it always did -- the safe side of this particular fence.
                catchError(() => of([] as string[])),
                shareReplay({ bufferSize: 1, refCount: false })
            );
        }

        return this._codes$.pipe(map((codes) => codes.indexOf(requestType) >= 0));
    }

    /** Drops the cached answer, for when the catalog itself has just been edited. */
    reset(): void {
        this._codes$ = undefined;
    }
}
