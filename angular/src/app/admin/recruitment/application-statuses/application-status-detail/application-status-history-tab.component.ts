import { Component, effect, inject, input, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityChangeServiceProxy, EntityChangeType } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { appStatusKeyFromHistoryValue } from './app-status-names';

/** Full type name ABP entity history records ApplicationStatus under (see HcmEntityHistoryTypes on the server). */
const APPLICATION_STATUS_TYPE = 'HCMv3.Recruitment.ApplicationStatus';
/** The entity property behind the Status field. */
const STATUS_PROPERTY = 'AppStatus';

interface StatusHistoryRow {
    changeTime: DateTime;
    userName: string | undefined;
    fromKey: string;
    toKey: string;
}

/**
 * Status History tab of an application's page: every change of its Status, newest first, read from ABP entity
 * history through ASP.NET Zero's own EntityChange service (nothing HCM-specific is stored). Saves that did not
 * touch the status are left out. Changes made before history was switched on for applications are not there.
 */
@Component({
    selector: 'applicationStatusHistoryTab',
    template: `
        <div [busyIf]="loading()">
            @if (rows().length === 0 && !loading()) {
                <div class="primeng-no-data">{{ 'NoData' | localize }}</div>
            } @else {
                <div class="table-responsive">
                    <table class="table table-row-dashed align-middle">
                        <thead>
                            <tr class="fw-bold text-muted">
                                <th style="min-width: 170px">{{ 'Time' | localize }}</th>
                                <th style="min-width: 150px">{{ 'UserName' | localize }}</th>
                                <th style="min-width: 150px">{{ 'OldValue' | localize }}</th>
                                <th style="min-width: 150px">{{ 'NewValue' | localize }}</th>
                            </tr>
                        </thead>
                        <tbody>
                            @for (row of rows(); track $index) {
                                <tr>
                                    <td>{{ row.changeTime | luxonFormat: 'yyyy-MM-dd HH:mm' }}</td>
                                    <td>{{ row.userName }}</td>
                                    <td>
                                        @if (row.fromKey) {
                                            <span class="text-muted text-decoration-line-through">
                                                {{ row.fromKey | localize }}
                                            </span>
                                        }
                                    </td>
                                    <td>
                                        <span class="badge badge-light-success fw-bold">
                                            {{ row.toKey | localize }}
                                        </span>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        </div>
    `,
    imports: [BusyIfDirective, LocalizePipe, LuxonFormatPipe],
})
export class ApplicationStatusHistoryTabComponent extends AppComponentBase {
    private _entityChangeService = inject(EntityChangeServiceProxy);

    readonly applicationStatusId = input.required<number>();
    /** Bumped by the page after the application is saved, so a status just changed shows up. */
    readonly refreshToken = input<number>(0);

    readonly rows = signal<StatusHistoryRow[]>([]);
    readonly loading = signal<boolean>(false);

    constructor() {
        super();
        effect(() => {
            this.refreshToken();
            this.load(this.applicationStatusId());
        });
    }

    private load(id: number): void {
        if (!id) {
            this.rows.set([]);
            return;
        }

        this.loading.set(true);
        this._entityChangeService
            .getEntityChangesByEntity(APPLICATION_STATUS_TYPE, String(id))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                const rows: StatusHistoryRow[] = [];
                for (const item of result.items ?? []) {
                    if (item.entityChange?.changeType === EntityChangeType.Deleted) {
                        continue;
                    }
                    const statusChange = item.entityPropertyChanges?.find((p) => p.propertyName === STATUS_PROPERTY);
                    if (!statusChange) {
                        continue;
                    }
                    const fromKey = appStatusKeyFromHistoryValue(statusChange.originalValue);
                    const toKey = appStatusKeyFromHistoryValue(statusChange.newValue);
                    if (fromKey === toKey) {
                        continue;
                    }
                    rows.push({
                        changeTime: item.entityChange.changeTime,
                        userName: item.entityChange.userName,
                        fromKey,
                        toKey,
                    });
                }
                this.rows.set(rows);
            });
    }
}
